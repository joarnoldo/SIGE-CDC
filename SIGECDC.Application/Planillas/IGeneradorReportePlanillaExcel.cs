using SIGECDC.Application.Archivos;

namespace SIGECDC.Application.Planillas;

public interface IGeneradorReportePlanillaExcel
{
    ArchivoDescarga Generar(ReportePlanilla reporte);
}
