namespace SIGECDC.Application.Planillas;

public class DetalleReportePlanilla
{
    public long IdDetallePlanilla { get; set; }
    public long IdColillaPago { get; set; }
    public string Puesto { get; set; } = string.Empty;
    public decimal SalarioBase { get; set; }
    public decimal IngresosNoSalariales { get; set; }
    public decimal BrutoPagadero { get; set; }
    public decimal CuotasObreras { get; set; }
    public decimal ImpuestoSalario { get; set; }
    public decimal OtrasDeducciones { get; set; }
    public decimal CargasPatronales { get; set; }
    public decimal RiesgosTrabajo { get; set; }
    public decimal OtrosCostosPatronales { get; set; }
    public decimal CostoLaboralTotal { get; set; }
    public IReadOnlyList<SegmentoPublicacionPlanilla> Segmentos { get; set; } = [];
    public IReadOnlyList<LineaPublicacionPlanilla> Lineas { get; set; } = [];
    public string CodigoColaborador { get; set; } = string.Empty;

    public string NombreColaborador { get; set; } = string.Empty;

    public string Departamento { get; set; } = string.Empty;

    public decimal SalarioProporcional { get; set; }

    public decimal TotalHorasExtra { get; set; }

    public decimal TotalBonos { get; set; }

    public decimal TotalBeneficiosConfigurables { get; set; }

    public decimal TotalAusencias { get; set; }

    public decimal SalarioBruto { get; set; }

    public decimal TotalDeducciones { get; set; }

    public decimal SalarioNeto { get; set; }
}
