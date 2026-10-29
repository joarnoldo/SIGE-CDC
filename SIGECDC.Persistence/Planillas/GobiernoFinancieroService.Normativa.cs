using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SIGECDC.Application.Planillas;
using SIGECDC.Domain.Planillas;
using SIGECDC.Domain.RecursosHumanos;

namespace SIGECDC.Persistence.Planillas;

public sealed partial class GobiernoFinancieroService
{
    public Task<long> ImportarNormativaAsync(PaqueteNormativo paquete, ReadOnlyMemory<byte> evidencia,
        string actor, CancellationToken ct = default) => EjecutarAsync(actor, async () =>
    {
        var f = paquete.Fuente;
        ValidarFuente(f, evidencia);
        if (paquete.Valores.Count + paquete.Minimos.Count + paquete.Tramos.Count + paquete.Creditos.Count == 0)
            throw new ArgumentException("El paquete normativo está vacío.");
        if (await contexto.VersionesNormativas.AnyAsync(v => v.Codigo == f.Codigo, ct))
            throw new InvalidOperationException("El código de versión ya existe; las cargas no sobrescriben versiones.");
        ValidarTramos(paquete.Tramos);
        var version = new VersionNormativa
        {
            Codigo = f.Codigo, Institucion = f.Institucion, Nombre = f.Nombre, FuenteOficialUrl = f.FuenteOficialUrl,
            IdentificadorPublicacion = f.IdentificadorPublicacion, FechaPublicacion = f.FechaPublicacion,
            FechaVigenciaInicio = f.Vigencia.Inicio, FechaVigenciaFin = f.Vigencia.Fin,
            HuellaContenido = f.HuellaContenido.ToUpperInvariant(), FechaCreacion = DateTime.UtcNow,
            CreadoPor = actor, EstadoRegistro = Inactivo
        };
        contexto.VersionesNormativas.Add(version);
        await contexto.SaveChangesAsync(ct);
        foreach (var d in paquete.Valores)
        {
            Clave(d.Codigo); Clave(d.ClaveAplicacion); Clave(d.CodigoBase, 80);
            Texto(d.Nombre, 200, "Nombre del parámetro"); Texto(d.Unidad, 30, "Unidad");
            Opcion(d.Sujeto, "Trabajador", "Patrono", "Estado", "Empresa");
            ValidarIdentidadOrdinaria(d);
            Dentro(f.Vigencia, d.Vigencia);
            DecimalExacto(d.Valor, 10, 0, 999999999999999999m);
            if (d.Unidad == "Proporcion") DecimalExacto(d.Valor, 10, 0, 1);
            if (d.Observaciones is not null) Texto(d.Observaciones, 500, "Observaciones");
            if (await contexto.ParametrosPlanilla.AnyAsync(p => p.Codigo == d.Codigo, ct))
                throw new InvalidOperationException("No se convierte un parámetro interno/semilla en normativa.");
            var parametro = await contexto.ParametrosNormativos.SingleOrDefaultAsync(p => p.Codigo == d.Codigo, ct);
            if (parametro is null)
            {
                parametro = new ParametroNormativo { Codigo = d.Codigo, Nombre = d.Nombre,
                    Institucion = f.Institucion, Sujeto = d.Sujeto, Unidad = d.Unidad, CodigoBase = d.CodigoBase,
                    EsObligatorio = d.EsObligatorio, EstadoRegistro = Activo };
                contexto.ParametrosNormativos.Add(parametro);
                await contexto.SaveChangesAsync(ct);
            }
            else if (parametro.Institucion != f.Institucion || parametro.Sujeto != d.Sujeto
                || parametro.Unidad != d.Unidad || parametro.CodigoBase != d.CodigoBase
                || parametro.EsObligatorio != d.EsObligatorio || parametro.Nombre != d.Nombre || parametro.EstadoRegistro != Activo)
                throw new InvalidOperationException("El paquete intenta reinterpretar un parámetro normativo existente: " + d.Codigo);
            var anteriores = await contexto.ValoresParametrosNormativos.Where(v =>
                v.IdParametroNormativo == parametro.IdParametroNormativo && v.ClaveAplicacion == d.ClaveAplicacion).ToListAsync(ct);
            SinSolape(d.Vigencia, anteriores.Select(v => Vigencia(v.FechaVigenciaInicio, v.FechaVigenciaFin)), d.Codigo + "/" + d.ClaveAplicacion);
            contexto.ValoresParametrosNormativos.Add(new ValorParametroNormativo
            {
                IdParametroNormativo = parametro.IdParametroNormativo, IdVersionNormativa = version.IdVersionNormativa,
                ClaveAplicacion = d.ClaveAplicacion, ValorDecimal = d.Valor, FechaVigenciaInicio = d.Vigencia.Inicio,
                FechaVigenciaFin = d.Vigencia.Fin, Observaciones = d.Observaciones
            });
            await contexto.SaveChangesAsync(ct);
        }
        foreach (var d in paquete.Minimos)
        {
            Clave(d.CodigoOcupacion); Texto(d.NombreOcupacion, 200, "Ocupación");
            Opcion(d.Periodicidad, "Diario", "Mensual");
            if (d.TipoJornada is not null) Opcion(d.TipoJornada, "Diurna", "Nocturna", "Mixta");
            Dentro(f.Vigencia, d.Vigencia); DecimalExacto(d.Monto, 2, 0.01m, 9999999999999999m);
            var ocupacion = await contexto.OcupacionesSalarioMinimo.SingleOrDefaultAsync(o => o.CodigoOficial == d.CodigoOcupacion, ct);
            if (ocupacion is null)
            {
                ocupacion = new OcupacionSalarioMinimo { CodigoOficial = d.CodigoOcupacion, Nombre = d.NombreOcupacion };
                contexto.OcupacionesSalarioMinimo.Add(ocupacion);
                await contexto.SaveChangesAsync(ct);
            }
            else if (ocupacion.Nombre != d.NombreOcupacion || ocupacion.EstadoRegistro != Activo)
                throw new InvalidOperationException("La ocupación existente no coincide con la fuente.");
            var anteriores = await contexto.SalariosMinimosVersion.Where(v => v.IdOcupacionSalarioMinimo == ocupacion.IdOcupacionSalarioMinimo
                && v.PeriodicidadReferencia == d.Periodicidad
                && (v.TipoJornadaAplicable == null || d.TipoJornada == null || v.TipoJornadaAplicable == d.TipoJornada)).ToListAsync(ct);
            SinSolape(d.Vigencia, anteriores.Select(v => Vigencia(v.FechaVigenciaInicio, v.FechaVigenciaFin)), d.CodigoOcupacion);
            contexto.SalariosMinimosVersion.Add(new SalarioMinimoVersion
            {
                IdOcupacionSalarioMinimo = ocupacion.IdOcupacionSalarioMinimo, IdVersionNormativa = version.IdVersionNormativa,
                PeriodicidadReferencia = d.Periodicidad, TipoJornadaAplicable = d.TipoJornada, Monto = d.Monto,
                FechaVigenciaInicio = d.Vigencia.Inicio, FechaVigenciaFin = d.Vigencia.Fin
            });
            await contexto.SaveChangesAsync(ct);
        }
        if (paquete.Tramos.Count != 0)
        {
            var anteriores = await contexto.TramosImpuestoSalario.Select(t => t.VersionNormativa!).Distinct().ToListAsync(ct);
            SinSolape(f.Vigencia, anteriores.Select(v => Vigencia(v.FechaVigenciaInicio, v.FechaVigenciaFin)), "TRAMOS_RENTA");
            contexto.TramosImpuestoSalario.AddRange(paquete.Tramos.Select(d => new TramoImpuestoSalario
            {
                IdVersionNormativa = version.IdVersionNormativa, NumeroOrden = d.Orden,
                LimiteInferior = d.Inferior, LimiteSuperior = d.Superior, Tasa = d.Tasa
            }));
        }
        foreach (var d in paquete.Creditos)
        {
            Clave(d.CodigoTipo, 80); Texto(d.Nombre, 200, "Crédito"); Texto(d.Requisitos, 1000, "Requisitos");
            Dentro(f.Vigencia, d.Vigencia); DecimalExacto(d.Monto, 2, 0, 9999999999999999m);
            var anteriores = await contexto.CreditosFiscalesNormativos.Where(c => c.CodigoTipo == d.CodigoTipo).ToListAsync(ct);
            SinSolape(d.Vigencia, anteriores.Select(v => Vigencia(v.FechaVigenciaInicio, v.FechaVigenciaFin)), d.CodigoTipo);
            contexto.CreditosFiscalesNormativos.Add(new CreditoFiscalNormativo
            {
                IdVersionNormativa = version.IdVersionNormativa, CodigoTipo = d.CodigoTipo, Nombre = d.Nombre,
                MontoUnitario = d.Monto, Requisitos = d.Requisitos, FechaVigenciaInicio = d.Vigencia.Inicio,
                FechaVigenciaFin = d.Vigencia.Fin
            });
            await contexto.SaveChangesAsync(ct);
        }
        await contexto.SaveChangesAsync(ct);
        Auditar(actor, "CargarNormativa", nameof(VersionNormativa), version.IdVersionNormativa,
            new { Paquete = paquete, HuellaRevision = await HuellaRevisionAsync(version.IdVersionNormativa, ct) });
        return version.IdVersionNormativa;
    }, ct);

    public Task<string> ObtenerHuellaRevisionAsync(long idVersion, string actor, CancellationToken ct = default)
        => EjecutarAsync(actor, () => HuellaRevisionAsync(idVersion, ct), ct);

    public async Task ActivarNormativaAsync(long idVersion, string huellaPaqueteRevisado, string revisor, CancellationToken ct = default)
        => await EjecutarAsync(revisor, async () =>
        {
            var version = await contexto.VersionesNormativas.SingleAsync(v => v.IdVersionNormativa == idVersion, ct);
            if (version.EstadoRegistro != Inactivo || version.FechaActivacion is not null || string.IsNullOrWhiteSpace(version.CreadoPor)
                || version.CreadoPor == revisor)
                throw new InvalidOperationException("La activación requiere una carga pendiente y un revisor distinto del creador.");
            await AutorizarAsync(version.CreadoPor, ct);
            var huella = await HuellaRevisionAsync(idVersion, ct);
            if (!string.Equals(huella, huellaPaqueteRevisado, StringComparison.Ordinal))
                throw new InvalidOperationException("La huella revisada no coincide con el paquete persistido.");
            if (huella != await HuellaAuditadaAsync(idVersion, "CargarNormativa", ct))
                throw new InvalidOperationException("El paquete fue alterado después de la carga; no puede activarse.");
            await ValidarIntegridadVersionAsync(idVersion, ct);
            version.EstadoRegistro = Activo;
            version.FechaActivacion = DateTime.UtcNow;
            version.ActivadoPor = revisor;
            Auditar(revisor, "ActivarNormativa", nameof(VersionNormativa), idVersion,
                new { version.Codigo, version.CreadoPor, version.ActivadoPor, version.FechaActivacion, HuellaRevision = huella });
            return true;
        }, ct);

    private async Task<string?> HuellaAuditadaAsync(long id, string accion, CancellationToken ct)
    {
        var registros = await contexto.BitacoraAuditoria.AsNoTracking().Where(b => b.Entidad == nameof(VersionNormativa)
            && b.IdRegistro == id.ToString() && b.Accion == accion).Select(b => b.ValoresNuevos).ToListAsync(ct);
        if (registros.Count != 1 || registros[0] is null) return null;
        try { using var json = JsonDocument.Parse(registros[0]!); return json.RootElement.GetProperty("HuellaRevision").GetString(); }
        catch (JsonException) { return null; }
        catch (KeyNotFoundException) { return null; }
    }

    private async Task ValidarIntegridadVersionAsync(long id, CancellationToken ct)
    {
        var valores = await contexto.ValoresParametrosNormativos.AsNoTracking().ToListAsync(ct);
        foreach (var v in valores.Where(v => v.IdVersionNormativa == id))
            SinSolape(Vigencia(v.FechaVigenciaInicio, v.FechaVigenciaFin), valores.Where(o => o.IdValorParametroNormativo != v.IdValorParametroNormativo
                && o.IdParametroNormativo == v.IdParametroNormativo && o.ClaveAplicacion == v.ClaveAplicacion)
                .Select(o => Vigencia(o.FechaVigenciaInicio, o.FechaVigenciaFin)), "PARÁMETRO NORMATIVO");
        var minimos = await contexto.SalariosMinimosVersion.AsNoTracking().ToListAsync(ct);
        foreach (var v in minimos.Where(v => v.IdVersionNormativa == id))
            SinSolape(Vigencia(v.FechaVigenciaInicio, v.FechaVigenciaFin), minimos.Where(o => o.IdSalarioMinimoVersion != v.IdSalarioMinimoVersion
                && o.IdOcupacionSalarioMinimo == v.IdOcupacionSalarioMinimo && o.PeriodicidadReferencia == v.PeriodicidadReferencia
                && (o.TipoJornadaAplicable is null || v.TipoJornadaAplicable is null || o.TipoJornadaAplicable == v.TipoJornadaAplicable))
                .Select(o => Vigencia(o.FechaVigenciaInicio, o.FechaVigenciaFin)), "MÍNIMO/NULL");
        var creditos = await contexto.CreditosFiscalesNormativos.AsNoTracking().ToListAsync(ct);
        foreach (var v in creditos.Where(v => v.IdVersionNormativa == id))
            SinSolape(Vigencia(v.FechaVigenciaInicio, v.FechaVigenciaFin), creditos.Where(o => o.IdCreditoFiscalNormativo != v.IdCreditoFiscalNormativo
                && o.CodigoTipo == v.CodigoTipo).Select(o => Vigencia(o.FechaVigenciaInicio, o.FechaVigenciaFin)), "CRÉDITO NORMATIVO");
        var versionesRenta = await contexto.TramosImpuestoSalario.AsNoTracking().Select(t => t.VersionNormativa!).Distinct().ToListAsync(ct);
        var renta = versionesRenta.SingleOrDefault(v => v.IdVersionNormativa == id);
        if (renta is not null) SinSolape(Vigencia(renta.FechaVigenciaInicio, renta.FechaVigenciaFin), versionesRenta.Where(v => v.IdVersionNormativa != id)
            .Select(v => Vigencia(v.FechaVigenciaInicio, v.FechaVigenciaFin)), "TRAMOS RENTA");
    }

    private async Task<string> HuellaRevisionAsync(long id, CancellationToken ct)
    {
        var v = await contexto.VersionesNormativas.AsNoTracking().SingleAsync(v => v.IdVersionNormativa == id, ct);
        return Huella(new
        {
            Fuente = new { v.Codigo, v.Institucion, v.Nombre, v.FuenteOficialUrl, v.IdentificadorPublicacion,
                v.FechaPublicacion, v.FechaVigenciaInicio, v.FechaVigenciaFin, v.HuellaContenido, v.CreadoPor },
            Valores = await contexto.ValoresParametrosNormativos.AsNoTracking().Where(x => x.IdVersionNormativa == id)
                .OrderBy(x => x.IdValorParametroNormativo).Select(x => new { x.ParametroNormativo!.Codigo, x.ParametroNormativo.Nombre,
                    x.ParametroNormativo.Institucion, x.ParametroNormativo.Sujeto, x.ParametroNormativo.Unidad,
                    x.ParametroNormativo.CodigoBase, x.ParametroNormativo.EsObligatorio, x.ClaveAplicacion,
                    x.ValorDecimal, x.FechaVigenciaInicio, x.FechaVigenciaFin, x.Observaciones }).ToListAsync(ct),
            Minimos = await contexto.SalariosMinimosVersion.AsNoTracking().Where(x => x.IdVersionNormativa == id)
                .OrderBy(x => x.IdSalarioMinimoVersion).Select(x => new { x.OcupacionSalarioMinimo!.CodigoOficial,
                    x.OcupacionSalarioMinimo.Nombre, x.PeriodicidadReferencia, x.TipoJornadaAplicable, x.Monto,
                    x.FechaVigenciaInicio, x.FechaVigenciaFin }).ToListAsync(ct),
            Tramos = await contexto.TramosImpuestoSalario.AsNoTracking().Where(x => x.IdVersionNormativa == id)
                .OrderBy(x => x.NumeroOrden).Select(x => new { x.NumeroOrden, x.LimiteInferior, x.LimiteSuperior, x.Tasa }).ToListAsync(ct),
            Creditos = await contexto.CreditosFiscalesNormativos.AsNoTracking().Where(x => x.IdVersionNormativa == id)
                .OrderBy(x => x.CodigoTipo).Select(x => new { x.CodigoTipo, x.Nombre, x.MontoUnitario, x.Requisitos,
                    x.FechaVigenciaInicio, x.FechaVigenciaFin, x.EstadoRegistro }).ToListAsync(ct)
        });
    }

    private static void ValidarFuente(FuenteNormativa fuente, ReadOnlyMemory<byte> evidencia)
    {
        Clave(fuente.Codigo); Texto(fuente.Institucion, 100, "Institución"); Texto(fuente.Nombre, 200, "Nombre");
        Texto(fuente.FuenteOficialUrl, 1000, "Fuente oficial"); fuente.Vigencia.Validar();
        if (!Uri.TryCreate(fuente.FuenteOficialUrl, UriKind.Absolute, out var url) || url.Scheme != "https" || url.UserInfo.Length != 0
            || !EsFuenteOficial(fuente.Institucion, url.Host)) throw new ArgumentException("La fuente debe corresponder a una institución oficial admitida.");
        if (fuente.IdentificadorPublicacion is not null) Texto(fuente.IdentificadorPublicacion, 150, "Publicación");
        if (fuente.FechaPublicacion?.TimeOfDay != null && fuente.FechaPublicacion.Value.TimeOfDay != TimeSpan.Zero)
            throw new ArgumentException("La fecha de publicación no admite hora.");
        if (evidencia.IsEmpty || !string.Equals(Convert.ToHexString(SHA256.HashData(evidencia.Span)), fuente.HuellaContenido, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("La evidencia no coincide con la huella SHA-256 de la fuente.");
    }
    private static bool EsFuenteOficial(string institucion, string host)
    {
        var dominio = institucion switch { "MTSS" or "CNS" => "mtss.go.cr", "CCSS" => "ccss.sa.cr",
            "HACIENDA" => "hacienda.go.cr", "PGR" => "pgrweb.go.cr", "INS" => "grupoins.com", _ => "" };
        return dominio.Length != 0 && (host == dominio || host.EndsWith("." + dominio, StringComparison.Ordinal));
    }
    private static void ValidarTramos(IReadOnlyList<TramoRentaCarga> tramos)
    {
        var ordenados = tramos.OrderBy(t => t.Orden).ToArray();
        for (var i = 0; i < ordenados.Length; i++)
        {
            var t = ordenados[i];
            DecimalExacto(t.Inferior, 2, 0, 9999999999999999m); DecimalExacto(t.Tasa, 10, 0, 1);
            if (t.Superior is { } superior) DecimalExacto(superior, 2, 0, 9999999999999999m);
            if (t.Orden != i + 1 || t.Inferior != (i == 0 ? 0 : ordenados[i - 1].Superior)
                || (i == ordenados.Length - 1 ? t.Superior is not null : t.Superior is null || t.Superior <= t.Inferior))
                throw new ArgumentException("Los tramos deben ser contiguos desde cero, ordenados y con un único tramo final abierto.");
        }
    }

    private static void ValidarIdentidadOrdinaria(ValorNormativoCarga dato)
    {
        if (!ClavesOrdinarias.Contains(dato.Codigo, StringComparer.Ordinal)) return;
        var bmc = dato.Codigo.StartsWith("BMC_", StringComparison.Ordinal);
        var sujeto = bmc ? "Empresa" : dato.Codigo.EndsWith("_TRABAJADOR", StringComparison.Ordinal) ? "Trabajador"
            : dato.Codigo.EndsWith("_ESTADO", StringComparison.Ordinal) ? "Estado" : "Patrono";
        var unidad = bmc ? "CRC_Mensual" : "Proporcion";
        var codigoBase = dato.Codigo.StartsWith("SEM_", StringComparison.Ordinal) || dato.Codigo == "BMC_SEM" ? "BASE_SEM"
            : dato.Codigo.StartsWith("IVM_", StringComparison.Ordinal) || dato.Codigo == "BMC_IVM" ? "BASE_IVM" : "REMUNERACION_CONTRIBUTIVA";
        if (dato.Sujeto != sujeto || dato.Unidad != unidad || dato.CodigoBase != codigoBase || !dato.EsObligatorio)
            throw new ArgumentException("Sujeto, unidad, base u obligatoriedad incompatibles con la clave ordinaria: " + dato.Codigo);
    }
}
