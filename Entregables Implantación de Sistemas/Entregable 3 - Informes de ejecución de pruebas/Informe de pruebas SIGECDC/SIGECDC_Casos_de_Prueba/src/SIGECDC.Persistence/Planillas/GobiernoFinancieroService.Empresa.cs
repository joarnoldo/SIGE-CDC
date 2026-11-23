using Microsoft.EntityFrameworkCore;
using SIGECDC.Application.Planillas;
using SIGECDC.Domain.Planillas;
using SIGECDC.Domain.RecursosHumanos;

namespace SIGECDC.Persistence.Planillas;

public sealed partial class GobiernoFinancieroService
{
    public Task<long> RegistrarConceptoAsync(ConceptoCarga datos, string actor, CancellationToken ct = default)
        => EjecutarAsync(actor, async () =>
        {
            Clave(datos.Codigo, 80); Texto(datos.Nombre, 150, "Nombre"); Texto(datos.ReferenciaSoporte, 500, "Soporte");
            datos.Vigencia.Validar();
            var concepto = await contexto.ConceptosPlanilla.SingleOrDefaultAsync(c => c.Codigo == datos.Codigo, ct);
            if (concepto is null)
            {
                concepto = new ConceptoPlanilla { Codigo = datos.Codigo, Nombre = datos.Nombre,
                    FechaCreacion = DateTime.UtcNow, CreadoPor = actor, EstadoRegistro = datos.Clasificacion is null ? Inactivo : Activo };
                contexto.ConceptosPlanilla.Add(concepto);
                await contexto.SaveChangesAsync(ct);
            }
            else if (concepto.Nombre != datos.Nombre)
                throw new InvalidOperationException("El catálogo no se reinterpreta al crear una versión.");
            if (datos.Clasificacion is { } d)
            {
                Opcion(d.Clasificacion, "IngresoSalarial", "IngresoNoSalarial", "DeduccionLegal", "DeduccionInterna", "CargaPatronal");
                Texto(d.TipoBase, 50, "Tipo de base"); Opcion(d.Unidad, "Monto", "Porcentaje", "Hora", "Día", "Cantidad");
                if (d.EsPatronal != (d.Clasificacion == "CargaPatronal")
                    || d.EsSalarial != (d.Clasificacion == "IngresoSalarial")
                    || (d.Clasificacion is "DeduccionLegal" or "DeduccionInterna" or "CargaPatronal"
                        && (d.EsContributivoCCSS || d.EsTributario)))
                    throw new ArgumentException("Clasificación y banderas del concepto incompatibles.");
                var anteriores = await contexto.ConceptosPlanillaVersion.Where(c => c.IdConceptoPlanilla == concepto.IdConceptoPlanilla).ToListAsync(ct);
                SinSolape(datos.Vigencia, anteriores.Select(v => Vigencia(v.FechaVigenciaInicio, v.FechaVigenciaFin)), datos.Codigo);
                contexto.ConceptosPlanillaVersion.Add(new ConceptoPlanillaVersion
                {
                    IdConceptoPlanilla = concepto.IdConceptoPlanilla, FechaVigenciaInicio = datos.Vigencia.Inicio,
                    FechaVigenciaFin = datos.Vigencia.Fin, Clasificacion = d.Clasificacion, EsSalarial = d.EsSalarial,
                    EsContributivoCCSS = d.EsContributivoCCSS, EsTributario = d.EsTributario, EsPatronal = d.EsPatronal,
                    TipoBase = d.TipoBase, UnidadPermitida = d.Unidad, PermiteMontoManual = d.PermiteMontoManual,
                    RequiereAutorizacion = d.RequiereAutorizacion
                });
                concepto.EstadoRegistro = Activo;
            }
            else if (await contexto.ConceptosPlanillaVersion.AnyAsync(c => c.IdConceptoPlanilla == concepto.IdConceptoPlanilla, ct))
                throw new InvalidOperationException("Una carga sin clasificación no puede alterar un concepto ya versionado.");
            if (datos.TipoIncidenciaLegada is { } nombre)
            {
                var tipo = await contexto.TiposIncidenciaPlanilla.SingleAsync(t => t.Nombre == nombre, ct);
                if (tipo.IdConceptoPlanilla is not null && tipo.IdConceptoPlanilla != concepto.IdConceptoPlanilla)
                    throw new InvalidOperationException("El tipo legado ya tiene otra asociación.");
                tipo.IdConceptoPlanilla = concepto.IdConceptoPlanilla;
            }
            await contexto.SaveChangesAsync(ct);
            Auditar(actor, "VersionarConcepto", nameof(ConceptoPlanilla), concepto.IdConceptoPlanilla, datos);
            return concepto.IdConceptoPlanilla;
        }, ct);

    public Task<long> RegistrarJornadaAsync(JornadaCarga datos, string actor, CancellationToken ct = default)
        => EjecutarAsync(actor, async () =>
        {
            Clave(datos.Codigo, 50); Texto(datos.Nombre, 150, "Nombre"); Texto(datos.ReferenciaSoporte, 500, "Soporte empresarial");
            Opcion(datos.Tipo, "Diurna", "Mixta", "Nocturna");
            // Límites de las unidades físicas, no máximos legales ni asignación de una jornada al cliente.
            DecimalExacto(datos.HorasDia, 4, 0.0001m, 24); DecimalExacto(datos.HorasSemana, 4, datos.HorasDia, 168);
            var anteriores = await contexto.JornadasLaborales.Where(j => j.Codigo == datos.Codigo).ToListAsync(ct);
            SinSolape(datos.Vigencia, anteriores.Select(j => Vigencia(j.FechaVigenciaInicio, j.FechaVigenciaFin)), datos.Codigo);
            var jornada = new JornadaLaboral { Codigo = datos.Codigo, Nombre = datos.Nombre, TipoJornada = datos.Tipo,
                HorasOrdinariasDia = datos.HorasDia, HorasOrdinariasSemana = datos.HorasSemana,
                FechaVigenciaInicio = datos.Vigencia.Inicio, FechaVigenciaFin = datos.Vigencia.Fin,
                FechaCreacion = DateTime.UtcNow, CreadoPor = actor };
            contexto.JornadasLaborales.Add(jornada);
            await contexto.SaveChangesAsync(ct);
            Auditar(actor, "VersionarJornada", nameof(JornadaLaboral), jornada.IdJornadaLaboral, datos);
            return jornada.IdJornadaLaboral;
        }, ct);

    public Task<long> RegistrarRiesgoTrabajoAsync(RiesgoTrabajoCarga datos, string actor, CancellationToken ct = default)
        => EjecutarAsync(actor, async () =>
        {
            Texto(datos.Poliza, 100, "Póliza"); Texto(datos.Actividad, 100, "Actividad");
            Texto(datos.ReferenciaDocumento, 500, "Documento de la póliza");
            DecimalExacto(datos.Tarifa, 10, 0, 99999999m);
            DecimalExacto(datos.Descuento, 10, 0, 1); DecimalExacto(datos.Recargo, 10, 0, 1);
            var anteriores = await contexto.ConfiguracionesRiesgoTrabajo.Where(r => r.NumeroPoliza == datos.Poliza && r.CodigoActividad == datos.Actividad).ToListAsync(ct);
            SinSolape(datos.Vigencia, anteriores.Select(r => Vigencia(r.FechaVigenciaInicio, r.FechaVigenciaFin)), "RT");
            if (datos.IdVersionNormativa is { } id)
            {
                var norma = await contexto.VersionesNormativas.SingleAsync(v => v.IdVersionNormativa == id, ct);
                if (!NormativaUtilizable(norma)) throw new InvalidOperationException("Normativa RT no revisada/activa.");
                Dentro(Vigencia(norma.FechaVigenciaInicio, norma.FechaVigenciaFin), datos.Vigencia);
            }
            var riesgo = new ConfiguracionRiesgoTrabajo { NumeroPoliza = datos.Poliza, CodigoActividad = datos.Actividad,
                Tarifa = datos.Tarifa, Descuento = datos.Descuento, Recargo = datos.Recargo,
                FechaVigenciaInicio = datos.Vigencia.Inicio, FechaVigenciaFin = datos.Vigencia.Fin,
                ReferenciaDocumento = datos.ReferenciaDocumento, IdVersionNormativa = datos.IdVersionNormativa,
                FechaCreacion = DateTime.UtcNow, CreadoPor = actor };
            contexto.ConfiguracionesRiesgoTrabajo.Add(riesgo);
            await contexto.SaveChangesAsync(ct);
            Auditar(actor, "VersionarRT", nameof(ConfiguracionRiesgoTrabajo), riesgo.IdConfiguracionRiesgoTrabajo, datos);
            return riesgo.IdConfiguracionRiesgoTrabajo;
        }, ct);

    public Task<long> AcreditarCreditoAsync(CreditoPersonalCarga datos, string actor, CancellationToken ct = default)
        => EjecutarAsync(actor, async () =>
        {
            Texto(datos.ReferenciaSoporte, 300, "Soporte personal aprobado");
            DecimalExacto(datos.Cantidad, 2, 0.01m, 999999m);
            if (!await contexto.Colaboradores.AnyAsync(c => c.IdColaborador == datos.IdColaborador && c.EstadoRegistro == Activo, ct))
                throw new InvalidOperationException("Colaborador no disponible.");
            var credito = await contexto.CreditosFiscalesNormativos.Include(c => c.VersionNormativa)
                .SingleAsync(c => c.IdCreditoFiscalNormativo == datos.IdCreditoNormativo, ct);
            if (credito.EstadoRegistro != Activo || !NormativaUtilizable(credito.VersionNormativa))
                throw new InvalidOperationException("Crédito normativo no disponible.");
            Dentro(Vigencia(credito.FechaVigenciaInicio, credito.FechaVigenciaFin), datos.Vigencia);
            var anteriores = await contexto.CreditosFiscalesColaborador.Where(c => c.IdColaborador == datos.IdColaborador
                && c.CreditoFiscalNormativo!.CodigoTipo == credito.CodigoTipo).ToListAsync(ct);
            SinSolape(datos.Vigencia, anteriores.Select(c => Vigencia(c.FechaVigenciaInicio, c.FechaVigenciaFin)), "CRÉDITO PERSONAL");
            var asignacion = new CreditoFiscalColaborador { IdColaborador = datos.IdColaborador,
                IdCreditoFiscalNormativo = datos.IdCreditoNormativo, CantidadAcreditada = datos.Cantidad,
                FechaVigenciaInicio = datos.Vigencia.Inicio, FechaVigenciaFin = datos.Vigencia.Fin,
                ReferenciaSoporte = datos.ReferenciaSoporte, AprobadoPor = actor, FechaAprobacion = DateTime.UtcNow };
            contexto.CreditosFiscalesColaborador.Add(asignacion);
            await contexto.SaveChangesAsync(ct);
            // La evidencia personal se referencia; no se copia su contenido a la bitácora.
            Auditar(actor, "AcreditarCredito", nameof(CreditoFiscalColaborador), asignacion.IdCreditoFiscalColaborador,
                new { datos.IdColaborador, datos.IdCreditoNormativo, datos.Cantidad, datos.Vigencia });
            return asignacion.IdCreditoFiscalColaborador;
        }, ct);

    public Task<int> RegistrarParametroInternoAsync(ParametroInternoCarga datos, string actor, CancellationToken ct = default)
        => EjecutarAsync(actor, async () =>
        {
            Clave(datos.Codigo); Texto(datos.Nombre, 150, "Nombre"); Texto(datos.ReferenciaSoporte, 300, "Soporte empresarial");
            Opcion(datos.Tipo, "Monto", "Porcentaje", "Cantidad", "Texto"); Opcion(datos.Naturaleza, "Beneficio", "Deduccion");
            datos.Vigencia.Validar();
            if ((datos.Tipo == "Texto") != (datos.ValorTexto is not null) || (datos.Tipo == "Texto") == (datos.ValorDecimal is not null))
                throw new ArgumentException("El valor debe corresponder al tipo del parámetro interno.");
            if (datos.ValorTexto is { } texto) Texto(texto, 255, "Valor textual");
            if (datos.ValorDecimal is { } valor) DecimalExacto(valor, 4, 0, 99999999999999m);
            if (await contexto.ParametrosNormativos.AnyAsync(p => p.Codigo == datos.Codigo, ct))
                throw new InvalidOperationException("Una clave normativa no puede cargarse como parámetro interno.");
            var concepto = await contexto.ConceptosPlanilla.Include(c => c.Versiones).SingleAsync(c => c.IdConceptoPlanilla == datos.IdConcepto, ct);
            var versiones = concepto.Versiones.Where(v => v.EstadoRegistro == Activo
                && Vigencia(v.FechaVigenciaInicio, v.FechaVigenciaFin).Contiene(datos.Vigencia)).ToArray();
            if (concepto.EstadoRegistro != Activo || versiones.Length != 1
                || (datos.Naturaleza == "Deduccion" ? versiones[0].Clasificacion != "DeduccionInterna"
                    : versiones[0].Clasificacion is not ("IngresoSalarial" or "IngresoNoSalarial")))
                throw new InvalidOperationException("El parámetro interno requiere un concepto empresarial clasificado y vigente.");
            var anteriores = await contexto.ParametrosPlanilla.Where(p => p.Codigo == datos.Codigo).ToListAsync(ct);
            if (anteriores.Any(p => p.FechaVigenciaInicio is null))
                throw new InvalidOperationException("Existe un parámetro legado sin vigencia; requiere clasificación explícita, no se reinterpreta.");
            SinSolape(datos.Vigencia, anteriores.Select(p => Vigencia(p.FechaVigenciaInicio!.Value, p.FechaVigenciaFin)), datos.Codigo);
            var parametro = new ParametroPlanilla { Codigo = datos.Codigo, Nombre = datos.Nombre, Descripcion = datos.ReferenciaSoporte,
                TipoParametro = datos.Tipo, Naturaleza = datos.Naturaleza, ValorDecimal = datos.ValorDecimal, ValorTexto = datos.ValorTexto,
                IdConceptoPlanilla = datos.IdConcepto, EsNormativo = false, EsEditable = false,
                FechaVigenciaInicio = datos.Vigencia.Inicio, FechaVigenciaFin = datos.Vigencia.Fin,
                FechaCreacion = DateTime.UtcNow, CreadoPor = actor };
            contexto.ParametrosPlanilla.Add(parametro);
            await contexto.SaveChangesAsync(ct);
            Auditar(actor, "VersionarParametroInterno", nameof(ParametroPlanilla), parametro.IdParametroPlanilla, datos);
            return parametro.IdParametroPlanilla;
        }, ct);

    public async Task HomologarPuestoAsync(int idPuesto, long idOcupacion, string soporte, string actor, CancellationToken ct = default)
        => await EjecutarAsync(actor, async () =>
        {
            Texto(soporte, 500, "Evidencia de homologación");
            var puesto = await contexto.Puestos.SingleAsync(p => p.IdPuesto == idPuesto, ct);
            if (puesto.EstadoRegistro != Activo) throw new InvalidOperationException("Puesto no disponible.");
            if (!await contexto.OcupacionesSalarioMinimo.AnyAsync(o => o.IdOcupacionSalarioMinimo == idOcupacion && o.EstadoRegistro == Activo, ct))
                throw new InvalidOperationException("Ocupación no disponible.");
            if (puesto.IdOcupacionSalarioMinimo is not null && puesto.IdOcupacionSalarioMinimo != idOcupacion)
                throw new InvalidOperationException("La homologación existente no se sustituye sin evaluar su evidencia histórica.");
            puesto.IdOcupacionSalarioMinimo = idOcupacion;
            Auditar(actor, "HomologarPuesto", nameof(Puesto), idPuesto, new { idOcupacion, soporte });
            return true;
        }, ct);

    public async Task AsociarContratoAsync(long idContrato, long idJornada, long idRiesgo, string soporte, string actor, CancellationToken ct = default)
        => await EjecutarAsync(actor, async () =>
        {
            Texto(soporte, 500, "Evidencia contractual");
            var contrato = await contexto.Contratos.SingleAsync(c => c.IdContrato == idContrato, ct);
            var jornada = await contexto.JornadasLaborales.SingleAsync(j => j.IdJornadaLaboral == idJornada && j.EstadoRegistro == Activo, ct);
            var rt = await contexto.ConfiguracionesRiesgoTrabajo.SingleAsync(r => r.IdConfiguracionRiesgoTrabajo == idRiesgo && r.EstadoRegistro == Activo, ct);
            if (!Vigencia(jornada.FechaVigenciaInicio, jornada.FechaVigenciaFin).Solapa(Vigencia(contrato.FechaInicio, contrato.FechaFin))
                || !Vigencia(rt.FechaVigenciaInicio, rt.FechaVigenciaFin).Solapa(Vigencia(contrato.FechaInicio, contrato.FechaFin)))
                throw new InvalidOperationException("La configuración no coincide con la vigencia contractual.");
            if ((contrato.IdJornadaLaboral is not null && contrato.IdJornadaLaboral != idJornada)
                || (contrato.IdConfiguracionRiesgoTrabajo is not null && contrato.IdConfiguracionRiesgoTrabajo != idRiesgo))
                throw new InvalidOperationException("No se sustituye una asociación contractual existente.");
            contrato.IdJornadaLaboral = idJornada; contrato.IdConfiguracionRiesgoTrabajo = idRiesgo;
            Auditar(actor, "AsociarConfiguracion", nameof(Contrato), idContrato, new { idJornada, idRiesgo, soporte });
            return true;
        }, ct);
}
