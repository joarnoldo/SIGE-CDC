using SIGECDC.Domain.Planillas;

namespace SIGECDC.Application.Planillas;

public sealed record NuevaCondicionLaboral(long IdContrato, VigenciaFinanciera Vigencia, decimal SalarioBase,
    int IdPuesto, int IdDepartamento, long IdOcupacionSalarioMinimo, long IdJornadaLaboral,
    string ReferenciaSoporte, long? IdUltimaVersionEsperada);
public sealed record CondicionLaboralResumen(long IdCondicionLaboralVersion, NuevaCondicionLaboral Condicion,
    DateTime FechaCreacion, string CreadoPor, string EstadoRegistro);

public interface ICondicionLaboralService
{
    Task<long> RegistrarCondicionAsync(NuevaCondicionLaboral solicitud, string actor, CancellationToken ct = default);
    Task<IReadOnlyList<CondicionLaboralResumen>> ConsultarCondicionesAsync(long idContrato, string actor, CancellationToken ct = default);
}
