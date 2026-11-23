using SIGECDC.Application.Archivos;

namespace SIGECDC.Application.Planillas;

public interface IGeneradorReportePlanillaPdf
{
    ArchivoDescarga Generar(ReportePlanilla reporte);
}
