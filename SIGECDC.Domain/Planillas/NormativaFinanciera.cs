using SIGECDC.Domain.RecursosHumanos;

namespace SIGECDC.Domain.Planillas;

public sealed class VersionNormativa
{
    public long IdVersionNormativa { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Institucion { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string FuenteOficialUrl { get; set; } = string.Empty;
    public string? IdentificadorPublicacion { get; set; }
    public DateTime? FechaPublicacion { get; set; }
    public DateTime FechaVigenciaInicio { get; set; }
    public DateTime? FechaVigenciaFin { get; set; }
    public string? HuellaContenido { get; set; }
    public DateTime FechaCreacion { get; set; }
    public string? CreadoPor { get; set; }
    public DateTime? FechaActivacion { get; set; }
    public string? ActivadoPor { get; set; }
    public string EstadoRegistro { get; set; } = "Activo";
}

public sealed class ParametroNormativo
{
    public long IdParametroNormativo { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Institucion { get; set; } = string.Empty;
    public string Sujeto { get; set; } = string.Empty;
    public string Unidad { get; set; } = string.Empty;
    public string CodigoBase { get; set; } = string.Empty;
    public bool EsObligatorio { get; set; }
    public string EstadoRegistro { get; set; } = "Activo";
}

public sealed class ValorParametroNormativo
{
    public long IdValorParametroNormativo { get; set; }
    public long IdParametroNormativo { get; set; }
    public long IdVersionNormativa { get; set; }
    public string ClaveAplicacion { get; set; } = string.Empty;
    public decimal ValorDecimal { get; set; }
    public DateTime FechaVigenciaInicio { get; set; }
    public DateTime? FechaVigenciaFin { get; set; }
    public string? Observaciones { get; set; }

    public ParametroNormativo? ParametroNormativo { get; set; }
    public VersionNormativa? VersionNormativa { get; set; }
}

public sealed class TramoImpuestoSalario
{
    public long IdTramoImpuestoSalario { get; set; }
    public long IdVersionNormativa { get; set; }
    public int NumeroOrden { get; set; }
    public decimal LimiteInferior { get; set; }
    public decimal? LimiteSuperior { get; set; }
    public decimal Tasa { get; set; }

    public VersionNormativa? VersionNormativa { get; set; }
}

public sealed class CreditoFiscalNormativo
{
    public long IdCreditoFiscalNormativo { get; set; }
    public long IdVersionNormativa { get; set; }
    public string CodigoTipo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public decimal MontoUnitario { get; set; }
    public string Requisitos { get; set; } = string.Empty;
    public DateTime FechaVigenciaInicio { get; set; }
    public DateTime? FechaVigenciaFin { get; set; }
    public string EstadoRegistro { get; set; } = "Activo";

    public VersionNormativa? VersionNormativa { get; set; }
}

public sealed class CreditoFiscalColaborador
{
    public long IdCreditoFiscalColaborador { get; set; }
    public long IdColaborador { get; set; }
    public long IdCreditoFiscalNormativo { get; set; }
    public decimal CantidadAcreditada { get; set; }
    public DateTime FechaVigenciaInicio { get; set; }
    public DateTime? FechaVigenciaFin { get; set; }
    public string ReferenciaSoporte { get; set; } = string.Empty;
    public string AprobadoPor { get; set; } = string.Empty;
    public DateTime FechaAprobacion { get; set; }
    public string EstadoRegistro { get; set; } = "Activo";

    public Colaborador? Colaborador { get; set; }
    public CreditoFiscalNormativo? CreditoFiscalNormativo { get; set; }
}

public sealed class ConfiguracionRiesgoTrabajo
{
    public long IdConfiguracionRiesgoTrabajo { get; set; }
    public string NumeroPoliza { get; set; } = string.Empty;
    public string CodigoActividad { get; set; } = string.Empty;
    public string? DescripcionActividad { get; set; }
    public decimal Tarifa { get; set; }
    public decimal Descuento { get; set; }
    public decimal Recargo { get; set; }
    public DateTime FechaVigenciaInicio { get; set; }
    public DateTime? FechaVigenciaFin { get; set; }
    public string ReferenciaDocumento { get; set; } = string.Empty;
    public long? IdVersionNormativa { get; set; }
    public DateTime FechaCreacion { get; set; }
    public string? CreadoPor { get; set; }
    public DateTime? FechaModificacion { get; set; }
    public string? ModificadoPor { get; set; }
    public string EstadoRegistro { get; set; } = "Activo";

    public VersionNormativa? VersionNormativa { get; set; }
}
