using Microsoft.EntityFrameworkCore;
using SIGECDC.Application.Archivos;
using SIGECDC.Application.RecursosHumanos;
using SIGECDC.Domain.RecursosHumanos;
using SIGECDC.Persistence.Identity;

namespace SIGECDC.Persistence.RecursosHumanos;

public sealed class ContratoDocumentoService(
    ApplicationDbContext contexto,
    IAlmacenamientoArchivosService almacenamientoArchivos) : IContratoDocumentoService
{
    private const string EstadoRegistroActivo = "Activo";
    private const string EstadoRegistroInactivo = "Inactivo";
    private const string EntidadColaborador = "Colaborador";
    private const string TipoDocumentoContrato = "Contrato";

    public async Task<ExpedienteDocumental> ObtenerExpedienteDocumentalAsync(long idColaborador, CancellationToken cancellationToken = default)
    {
        var colaborador = await contexto.Colaboradores
            .AsNoTracking()
            .Include(registro => registro.EstadoLaboral)
            .Include(registro => registro.Departamento)
            .Include(registro => registro.Puesto)
            .Where(registro => registro.IdColaborador == idColaborador
                && registro.EstadoRegistro == EstadoRegistroActivo)
            .Select(registro => new ColaboradorDetalle
            {
                IdColaborador = registro.IdColaborador,
                CodigoColaborador = registro.CodigoColaborador,
                TipoIdentificacion = registro.TipoIdentificacion,
                Identificacion = registro.Identificacion,
                Nombre = registro.Nombre,
                PrimerApellido = registro.PrimerApellido,
                SegundoApellido = registro.SegundoApellido,
                FechaNacimiento = registro.FechaNacimiento,
                CorreoElectronico = registro.CorreoElectronico,
                Telefono = registro.Telefono,
                Direccion = registro.Direccion,
                FechaIngreso = registro.FechaIngreso,
                FechaSalida = registro.FechaSalida,
                IdEstadoLaboral = registro.IdEstadoLaboral,
                EstadoLaboral = registro.EstadoLaboral != null ? registro.EstadoLaboral.Nombre : string.Empty,
                IdDepartamento = registro.IdDepartamento,
                Departamento = registro.Departamento != null ? registro.Departamento.Nombre : string.Empty,
                IdPuesto = registro.IdPuesto,
                Puesto = registro.Puesto != null ? registro.Puesto.Nombre : string.Empty,
                Observaciones = registro.Observaciones
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (colaborador is null)
        {
            return new ExpedienteDocumental();
        }

        return new ExpedienteDocumental
        {
            Colaborador = colaborador,
            Contratos = await ObtenerContratosAsync(idColaborador, cancellationToken),
            Documentos = await ObtenerDocumentosAsync(idColaborador, cancellationToken)
        };
    }

    public async Task<ContratoDetalle?> ObtenerContratoPorIdAsync(long idContrato, CancellationToken cancellationToken = default)
    {
        return await contexto.Contratos
            .AsNoTracking()
            .Where(contrato => contrato.IdContrato == idContrato
                && contrato.EstadoRegistro == EstadoRegistroActivo)
            .Select(contrato => new ContratoDetalle
            {
                IdContrato = contrato.IdContrato,
                IdColaborador = contrato.IdColaborador,
                TipoContrato = contrato.TipoContrato,
                FechaInicio = contrato.FechaInicio,
                FechaFin = contrato.FechaFin,
                SalarioBase = contrato.SalarioBase,
                Jornada = contrato.Jornada,
                PeriodicidadPago = contrato.PeriodicidadPago,
                EstadoContrato = contrato.EstadoContrato,
                Observaciones = contrato.Observaciones
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<OpcionCatalogo>> ObtenerTiposDocumentoAsync(CancellationToken cancellationToken = default)
    {
        return await contexto.TiposDocumento
            .AsNoTracking()
            .Where(tipo => tipo.EstadoRegistro == EstadoRegistroActivo)
            .OrderBy(tipo => tipo.Nombre)
            .Select(tipo => new OpcionCatalogo(tipo.IdTipoDocumento, tipo.Nombre, tipo.Descripcion))
            .ToListAsync(cancellationToken);
    }

    public async Task<long> RegistrarContratoAsync(
        SolicitudContrato solicitud,
        SolicitudArchivoDocumento? archivoContrato,
        string? idUsuarioActual = null,
        CancellationToken cancellationToken = default)
    {
        var datos = await ValidarContratoAsync(solicitud, cancellationToken);
        DocumentoArchivo? documentoContrato = null;

        if (archivoContrato is not null)
        {
            var idTipoContrato = await ObtenerIdTipoDocumentoContratoAsync(cancellationToken);
            archivoContrato.IdColaborador = solicitud.IdColaborador;
            archivoContrato.IdTipoDocumento = idTipoContrato;
            documentoContrato = await PrepararDocumentoAsync(archivoContrato, idUsuarioActual, cancellationToken);
        }

        var contrato = new Contrato
        {
            IdColaborador = solicitud.IdColaborador,
            TipoContrato = datos.TipoContrato,
            FechaInicio = datos.FechaInicio,
            FechaFin = datos.FechaFin,
            SalarioBase = solicitud.SalarioBase,
            Jornada = datos.Jornada,
            PeriodicidadPago = datos.PeriodicidadPago,
            EstadoContrato = datos.EstadoContrato,
            Observaciones = datos.Observaciones,
            FechaCreacion = DateTime.Now,
            CreadoPor = idUsuarioActual,
            EstadoRegistro = EstadoRegistroActivo
        };

        contexto.Contratos.Add(contrato);
        if (documentoContrato is not null)
        {
            contexto.DocumentosArchivo.Add(documentoContrato);
        }

        await contexto.SaveChangesAsync(cancellationToken);

        return contrato.IdContrato;
    }

    public async Task ActualizarContratoAsync(
        long idContrato,
        SolicitudContrato solicitud,
        string? idUsuarioActual = null,
        CancellationToken cancellationToken = default)
    {
        var contrato = await contexto.Contratos
            .FirstOrDefaultAsync(
                contrato => contrato.IdContrato == idContrato
                    && contrato.EstadoRegistro == EstadoRegistroActivo,
                cancellationToken);

        if (contrato is null)
        {
            throw new InvalidOperationException("No se encontro el contrato solicitado.");
        }

        var datos = await ValidarContratoAsync(solicitud, cancellationToken);

        contrato.IdColaborador = solicitud.IdColaborador;
        contrato.TipoContrato = datos.TipoContrato;
        contrato.FechaInicio = datos.FechaInicio;
        contrato.FechaFin = datos.FechaFin;
        contrato.SalarioBase = solicitud.SalarioBase;
        contrato.Jornada = datos.Jornada;
        contrato.PeriodicidadPago = datos.PeriodicidadPago;
        contrato.EstadoContrato = datos.EstadoContrato;
        contrato.Observaciones = datos.Observaciones;
        contrato.FechaModificacion = DateTime.Now;
        contrato.ModificadoPor = idUsuarioActual;

        await contexto.SaveChangesAsync(cancellationToken);
    }

    public async Task<long> AdjuntarDocumentoAsync(
        SolicitudArchivoDocumento solicitud,
        string? idUsuarioActual = null,
        CancellationToken cancellationToken = default)
    {
        var documento = await PrepararDocumentoAsync(solicitud, idUsuarioActual, cancellationToken);

        contexto.DocumentosArchivo.Add(documento);
        await contexto.SaveChangesAsync(cancellationToken);

        return documento.IdDocumentoArchivo;
    }

    private async Task<DocumentoArchivo> PrepararDocumentoAsync(
        SolicitudArchivoDocumento solicitud,
        string? idUsuarioActual,
        CancellationToken cancellationToken)
    {
        await ValidarSolicitudDocumentoAsync(solicitud, cancellationToken);

        var archivo = await almacenamientoArchivos.GuardarAsync(
            solicitud.Contenido,
            solicitud.NombreOriginal,
            solicitud.MimeType,
            solicitud.TamanoBytes,
            ObtenerCarpetaColaborador(solicitud.IdColaborador),
            cancellationToken);

        return new DocumentoArchivo
        {
            EntidadRelacionada = EntidadColaborador,
            IdEntidadRelacionada = solicitud.IdColaborador,
            IdTipoDocumento = solicitud.IdTipoDocumento,
            NombreOriginal = solicitud.NombreOriginal.Trim(),
            NombreAlmacenado = archivo.NombreAlmacenado,
            RutaRelativa = archivo.RutaRelativa,
            MimeType = archivo.MimeType,
            TamanoBytes = archivo.TamanoBytes,
            FechaCarga = DateTime.Now,
            CargadoPor = idUsuarioActual,
            EstadoRegistro = EstadoRegistroActivo
        };
    }

    public async Task<long> ReemplazarDocumentoAsync(
        long idDocumentoAnterior,
        SolicitudArchivoDocumento solicitud,
        string? idUsuarioActual = null,
        CancellationToken cancellationToken = default)
    {
        var documentoAnterior = await contexto.DocumentosArchivo
            .FirstOrDefaultAsync(
                documento => documento.IdDocumentoArchivo == idDocumentoAnterior
                    && documento.EntidadRelacionada == EntidadColaborador
                    && documento.EstadoRegistro == EstadoRegistroActivo,
                cancellationToken);

        if (documentoAnterior is null)
        {
            throw new InvalidOperationException("No se encontro el documento vigente para reemplazar.");
        }

        solicitud.IdColaborador = documentoAnterior.IdEntidadRelacionada;
        solicitud.IdTipoDocumento = documentoAnterior.IdTipoDocumento;

        documentoAnterior.EstadoRegistro = EstadoRegistroInactivo;

        var idNuevoDocumento = await AdjuntarDocumentoAsync(solicitud, idUsuarioActual, cancellationToken);
        await contexto.SaveChangesAsync(cancellationToken);

        return idNuevoDocumento;
    }

    public async Task<DocumentoArchivoDetalle?> ObtenerDocumentoPorIdAsync(long idDocumentoArchivo, CancellationToken cancellationToken = default)
    {
        return await contexto.DocumentosArchivo
            .AsNoTracking()
            .Include(documento => documento.TipoDocumento)
            .Where(documento => documento.IdDocumentoArchivo == idDocumentoArchivo
                && documento.EntidadRelacionada == EntidadColaborador
                && documento.EstadoRegistro == EstadoRegistroActivo)
            .Select(documento => new DocumentoArchivoDetalle
            {
                IdDocumentoArchivo = documento.IdDocumentoArchivo,
                EntidadRelacionada = documento.EntidadRelacionada,
                IdEntidadRelacionada = documento.IdEntidadRelacionada,
                IdTipoDocumento = documento.IdTipoDocumento,
                TipoDocumento = documento.TipoDocumento != null ? documento.TipoDocumento.Nombre : string.Empty,
                NombreOriginal = documento.NombreOriginal,
                NombreAlmacenado = documento.NombreAlmacenado,
                RutaRelativa = documento.RutaRelativa,
                MimeType = documento.MimeType,
                TamanoBytes = documento.TamanoBytes,
                FechaCarga = documento.FechaCarga,
                EstadoRegistro = documento.EstadoRegistro
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ArchivoDescarga?> AbrirDocumentoAsync(long idDocumentoArchivo, CancellationToken cancellationToken = default)
    {
        var documento = await ObtenerDocumentoPorIdAsync(idDocumentoArchivo, cancellationToken);

        if (documento is null)
        {
            return null;
        }

        var contenido = await almacenamientoArchivos.AbrirLecturaAsync(documento.RutaRelativa, cancellationToken);
        return new ArchivoDescarga(contenido, documento.NombreOriginal, documento.MimeType);
    }

    private async Task<IReadOnlyList<ContratoResumen>> ObtenerContratosAsync(long idColaborador, CancellationToken cancellationToken)
    {
        var contratos = await contexto.Contratos
            .AsNoTracking()
            .Include(contrato => contrato.Colaborador)
            .Where(contrato => contrato.IdColaborador == idColaborador
                && contrato.EstadoRegistro == EstadoRegistroActivo)
            .OrderByDescending(contrato => contrato.FechaInicio)
            .Select(contrato => new
            {
                contrato.IdContrato,
                contrato.IdColaborador,
                Nombre = contrato.Colaborador != null ? contrato.Colaborador.Nombre : string.Empty,
                PrimerApellido = contrato.Colaborador != null ? contrato.Colaborador.PrimerApellido : string.Empty,
                SegundoApellido = contrato.Colaborador != null ? contrato.Colaborador.SegundoApellido : null,
                contrato.TipoContrato,
                contrato.FechaInicio,
                contrato.FechaFin,
                contrato.SalarioBase,
                contrato.Jornada,
                contrato.PeriodicidadPago,
                contrato.EstadoContrato
            })
            .ToListAsync(cancellationToken);

        return contratos
            .Select(contrato => new ContratoResumen
            {
                IdContrato = contrato.IdContrato,
                IdColaborador = contrato.IdColaborador,
                NombreColaborador = ConstruirNombreCompleto(contrato.Nombre, contrato.PrimerApellido, contrato.SegundoApellido),
                TipoContrato = contrato.TipoContrato,
                FechaInicio = contrato.FechaInicio,
                FechaFin = contrato.FechaFin,
                SalarioBase = contrato.SalarioBase,
                Jornada = contrato.Jornada,
                PeriodicidadPago = contrato.PeriodicidadPago,
                EstadoContrato = contrato.EstadoContrato
            })
            .ToList();
    }

    private async Task<IReadOnlyList<DocumentoArchivoResumen>> ObtenerDocumentosAsync(long idColaborador, CancellationToken cancellationToken)
    {
        return await contexto.DocumentosArchivo
            .AsNoTracking()
            .Include(documento => documento.TipoDocumento)
            .Where(documento => documento.EntidadRelacionada == EntidadColaborador
                && documento.IdEntidadRelacionada == idColaborador)
            .OrderByDescending(documento => documento.FechaCarga)
            .Select(documento => new DocumentoArchivoResumen
            {
                IdDocumentoArchivo = documento.IdDocumentoArchivo,
                IdColaborador = documento.IdEntidadRelacionada,
                IdTipoDocumento = documento.IdTipoDocumento,
                TipoDocumento = documento.TipoDocumento != null ? documento.TipoDocumento.Nombre : string.Empty,
                NombreOriginal = documento.NombreOriginal,
                MimeType = documento.MimeType,
                TamanoBytes = documento.TamanoBytes,
                FechaCarga = documento.FechaCarga,
                EstadoRegistro = documento.EstadoRegistro
            })
            .ToListAsync(cancellationToken);
    }

    private async Task<DatosContratoLimpios> ValidarContratoAsync(SolicitudContrato solicitud, CancellationToken cancellationToken)
    {
        if (solicitud.FechaInicio is null)
        {
            throw new ArgumentException("La fecha de inicio del contrato es obligatoria.");
        }

        var colaboradorExiste = await contexto.Colaboradores
            .AnyAsync(
                colaborador => colaborador.IdColaborador == solicitud.IdColaborador
                    && colaborador.EstadoRegistro == EstadoRegistroActivo,
                cancellationToken);

        if (!colaboradorExiste)
        {
            throw new ArgumentException("El colaborador indicado no existe o no esta activo.");
        }

        var periodicidad = LimpiarObligatorio(solicitud.PeriodicidadPago, "La periodicidad de pago es obligatoria.");
        var estadoContrato = LimpiarObligatorio(solicitud.EstadoContrato, "El estado del contrato es obligatorio.");

        if (!PeriodicidadesPago.EsValida(periodicidad))
        {
            throw new ArgumentException("La periodicidad de pago indicada no es valida.");
        }

        if (!EstadosContrato.EsValido(estadoContrato))
        {
            throw new ArgumentException("El estado del contrato indicado no es valido.");
        }

        if (solicitud.FechaFin.HasValue && solicitud.FechaFin.Value.Date < solicitud.FechaInicio.Value.Date)
        {
            throw new ArgumentException("La fecha de fin no puede ser anterior a la fecha de inicio.");
        }

        return new DatosContratoLimpios(
            LimpiarObligatorio(solicitud.TipoContrato, "El tipo de contrato es obligatorio."),
            solicitud.FechaInicio.Value.Date,
            solicitud.FechaFin?.Date,
            LimpiarOpcional(solicitud.Jornada),
            periodicidad,
            estadoContrato,
            LimpiarOpcional(solicitud.Observaciones));
    }

    private async Task ValidarSolicitudDocumentoAsync(SolicitudArchivoDocumento solicitud, CancellationToken cancellationToken)
    {
        var colaboradorExiste = await contexto.Colaboradores
            .AnyAsync(
                colaborador => colaborador.IdColaborador == solicitud.IdColaborador
                    && colaborador.EstadoRegistro == EstadoRegistroActivo,
                cancellationToken);

        if (!colaboradorExiste)
        {
            throw new ArgumentException("El colaborador indicado no existe o no esta activo.");
        }

        var tipoDocumentoExiste = await contexto.TiposDocumento
            .AnyAsync(
                tipo => tipo.IdTipoDocumento == solicitud.IdTipoDocumento
                    && tipo.EstadoRegistro == EstadoRegistroActivo,
                cancellationToken);

        if (!tipoDocumentoExiste)
        {
            throw new ArgumentException("El tipo de documento indicado no esta disponible.");
        }
    }

    private async Task<int> ObtenerIdTipoDocumentoContratoAsync(CancellationToken cancellationToken)
    {
        var idTipoDocumento = await contexto.TiposDocumento
            .Where(tipo => tipo.Nombre == TipoDocumentoContrato
                && tipo.EstadoRegistro == EstadoRegistroActivo)
            .Select(tipo => tipo.IdTipoDocumento)
            .FirstOrDefaultAsync(cancellationToken);

        if (idTipoDocumento <= 0)
        {
            throw new InvalidOperationException("No existe el tipo de documento Contrato en el catalogo.");
        }

        return idTipoDocumento;
    }

    private static string ObtenerCarpetaColaborador(long idColaborador)
    {
        return $"colaboradores/{idColaborador}";
    }

    private static string ConstruirNombreCompleto(string nombre, string primerApellido, string? segundoApellido)
    {
        return string.IsNullOrWhiteSpace(segundoApellido)
            ? $"{nombre} {primerApellido}"
            : $"{nombre} {primerApellido} {segundoApellido}";
    }

    private static string LimpiarObligatorio(string valor, string mensajeError)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            throw new ArgumentException(mensajeError);
        }

        return valor.Trim();
    }

    private static string? LimpiarOpcional(string? valor)
    {
        return string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
    }

    private sealed record DatosContratoLimpios(
        string TipoContrato,
        DateTime FechaInicio,
        DateTime? FechaFin,
        string? Jornada,
        string PeriodicidadPago,
        string EstadoContrato,
        string? Observaciones);
}
