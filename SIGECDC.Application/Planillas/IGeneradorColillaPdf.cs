using SIGECDC.Application.Archivos;

namespace SIGECDC.Application.Planillas;

public interface IGeneradorColillaPdf
{
    ArchivoDescarga Generar(ColillaPagoDetalle colilla);
}
