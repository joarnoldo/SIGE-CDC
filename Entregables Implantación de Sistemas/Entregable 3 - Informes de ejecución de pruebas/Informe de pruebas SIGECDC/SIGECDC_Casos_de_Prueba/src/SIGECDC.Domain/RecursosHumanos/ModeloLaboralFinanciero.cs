using SIGECDC.Domain.Planillas;

namespace SIGECDC.Domain.RecursosHumanos;

public sealed class JornadaLaboral
{
    public long IdJornadaLaboral { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string TipoJornada { get; set; } = string.Empty;
    public decimal HorasOrdinariasDia { get; set; }
    public decimal HorasOrdinariasSemana { get; set; }
    public DateTime FechaVigenciaInicio { get; set; }
    public DateTime? FechaVigenciaFin { get; set; }
    public DateTime FechaCreacion { get; set; }
    public string? CreadoPor { get; set; }
    public DateTime? FechaModificacion { get; set; }
    public string? ModificadoPor { get; set; }
    public string EstadoRegistro { get; set; } = "Activo";
}

public sealed class OcupacionSalarioMinimo
{
    public long IdOcupacionSalarioMinimo { get; set; }
    public string CodigoOficial { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string EstadoRegistro { get; set; } = "Activo";
}

public sealed class SalarioMinimoVersion
{
    public long IdSalarioMinimoVersion { get; set; }
    public long IdOcupacionSalarioMinimo { get; set; }
    public long IdVersionNormativa { get; set; }
    public string PeriodicidadReferencia { get; set; } = string.Empty;
    public string? TipoJornadaAplicable { get; set; }
    public decimal Monto { get; set; }
    public DateTime FechaVigenciaInicio { get; set; }
    public DateTime? FechaVigenciaFin { get; set; }

    public OcupacionSalarioMinimo? OcupacionSalarioMinimo { get; set; }
    public VersionNormativa? VersionNormativa { get; set; }
}
