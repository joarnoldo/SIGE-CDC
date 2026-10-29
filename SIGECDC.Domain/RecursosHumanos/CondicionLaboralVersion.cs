namespace SIGECDC.Domain.RecursosHumanos;

public sealed class CondicionLaboralVersion
{
    public long IdCondicionLaboralVersion { get; set; }
    public long IdContrato { get; set; }
    public DateTime FechaVigenciaInicio { get; set; }
    public DateTime? FechaVigenciaFin { get; set; }
    public decimal SalarioBase { get; set; }
    public int IdPuesto { get; set; }
    public int IdDepartamento { get; set; }
    public long IdOcupacionSalarioMinimo { get; set; }
    public long IdJornadaLaboral { get; set; }
    public string ReferenciaSoporte { get; set; } = string.Empty;
    public DateTime FechaCreacion { get; set; }
    public string CreadoPor { get; set; } = string.Empty;
    public string EstadoRegistro { get; set; } = "Activo";
    public Contrato? Contrato { get; set; }
    public Puesto? Puesto { get; set; }
    public Departamento? Departamento { get; set; }
    public OcupacionSalarioMinimo? OcupacionSalarioMinimo { get; set; }
    public JornadaLaboral? JornadaLaboral { get; set; }
}
