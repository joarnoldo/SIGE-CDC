using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SIGECDC.Application.Planillas;
using SIGECDC.Domain.Auditoria;
using SIGECDC.Domain.Planillas;
using SIGECDC.Domain.RecursosHumanos;
using SIGECDC.Persistence.Identity;

namespace SIGECDC.Persistence.Planillas;

public sealed class PlanillaService(ApplicationDbContext contexto) : IPlanillaService
{
    private const string EstadoActivo = "Activo";
    private const string EstadoContratoActivo = "Activo";

    public async Task<IReadOnlyList<PeriodoPlanillaResumen>> ObtenerPeriodosAsync(
        CancellationToken cancellationToken = default)
    {
        return await contexto.PeriodosPlanilla
            .AsNoTracking()
            .Where(periodo => periodo.EstadoRegistro == EstadoActivo)
            .OrderByDescending(periodo => periodo.FechaInicio)
            .ThenByDescending(periodo => periodo.IdPeriodoPlanilla)
            .Select(periodo => new PeriodoPlanillaResumen
            {
                IdPeriodoPlanilla = periodo.IdPeriodoPlanilla,
                CodigoPeriodo = periodo.CodigoPeriodo,
                Nombre = periodo.Nombre,
                TipoPeriodo = periodo.TipoPeriodo,
                FechaInicio = periodo.FechaInicio,
                FechaFin = periodo.FechaFin,
                EstadoPlanilla = periodo.EstadoPlanilla != null ? periodo.EstadoPlanilla.Nombre : string.Empty,
                CantidadIncidencias = contexto.IncidenciasPlanilla.Count(incidencia =>
                    incidencia.IdPeriodoPlanilla == periodo.IdPeriodoPlanilla
                    && incidencia.EstadoRegistro == EstadoActivo),
                TienePlanilla = periodo.Planilla != null,
                SalarioNetoTotal = periodo.Planilla != null ? periodo.Planilla.SalarioNetoTotal : 0m
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<long> CrearPeriodoAsync(
        SolicitudPeriodoPlanilla solicitud,
        string? idUsuarioActual = null,
        CancellationToken cancellationToken = default)
    {
        var datos = DatosPeriodoLimpios.DesdeSolicitud(solicitud);
        var estadoBorrador = await ObtenerEstadoAsync(EstadosPlanilla.Borrador, cancellationToken);

        var codigoExiste = await contexto.PeriodosPlanilla.AnyAsync(
            periodo => periodo.CodigoPeriodo == datos.CodigoPeriodo,
            cancellationToken);

        if (codigoExiste)
        {
            throw new InvalidOperationException("Ya existe un período de planilla con el código indicado.");
        }

        await using var transaccion = await contexto.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var ahora = DateTime.Now;
            var periodo = new PeriodoPlanilla
            {
                CodigoPeriodo = datos.CodigoPeriodo,
                Nombre = datos.Nombre,
                TipoPeriodo = datos.TipoPeriodo,
                FechaInicio = datos.FechaInicio,
                FechaFin = datos.FechaFin,
                IdEstadoPlanilla = estadoBorrador.IdEstadoPlanilla,
                Observaciones = datos.Observaciones,
                FechaCreacion = ahora,
                CreadoPor = LimpiarIdUsuario(idUsuarioActual),
                EstadoRegistro = EstadoActivo
            };

            contexto.PeriodosPlanilla.Add(periodo);
            await contexto.SaveChangesAsync(cancellationToken);

            contexto.BitacoraAuditoria.Add(new BitacoraAuditoria
            {
                IdUsuario = LimpiarIdUsuario(idUsuarioActual),
                FechaHora = ahora,
                Accion = "CREAR_PERIODO",
                Entidad = "PeriodoPlanilla",
                IdRegistro = periodo.IdPeriodoPlanilla.ToString(),
                ValoresNuevos = JsonSerializer.Serialize(new
                {
                    periodo.CodigoPeriodo,
                    periodo.Nombre,
                    periodo.TipoPeriodo,
                    periodo.FechaInicio,
                    periodo.FechaFin,
                    Estado = EstadosPlanilla.Borrador
                }),
                Observacion = "Período de planilla creado en estado Borrador."
            });

            await contexto.SaveChangesAsync(cancellationToken);
            await transaccion.CommitAsync(cancellationToken);
            return periodo.IdPeriodoPlanilla;
        }
        catch
        {
            await transaccion.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    public async Task<PeriodoPlanillaDetalle?> ObtenerDetallePeriodoAsync(
        long idPeriodoPlanilla,
        CancellationToken cancellationToken = default)
    {
        var periodo = await contexto.PeriodosPlanilla
            .AsNoTracking()
            .Where(registro => registro.IdPeriodoPlanilla == idPeriodoPlanilla
                && registro.EstadoRegistro == EstadoActivo)
            .Select(registro => new PeriodoPlanillaDetalle
            {
                IdPeriodoPlanilla = registro.IdPeriodoPlanilla,
                CodigoPeriodo = registro.CodigoPeriodo,
                Nombre = registro.Nombre,
                TipoPeriodo = registro.TipoPeriodo,
                FechaInicio = registro.FechaInicio,
                FechaFin = registro.FechaFin,
                EstadoPlanilla = registro.EstadoPlanilla != null ? registro.EstadoPlanilla.Nombre : string.Empty,
                Observaciones = registro.Observaciones,
                CantidadIncidencias = contexto.IncidenciasPlanilla.Count(incidencia =>
                    incidencia.IdPeriodoPlanilla == registro.IdPeriodoPlanilla
                    && incidencia.EstadoRegistro == EstadoActivo),
                IdPlanilla = registro.Planilla != null ? registro.Planilla.IdPlanilla : null,
                FechaCalculo = registro.Planilla != null ? registro.Planilla.FechaCalculo : null,
                FechaAprobacion = registro.Planilla != null ? registro.Planilla.FechaAprobacion : null,
                FechaCierre = registro.Planilla != null ? registro.Planilla.FechaCierre : null,
                SalarioBrutoTotal = registro.Planilla != null ? registro.Planilla.SalarioBrutoTotal : 0m,
                DeduccionesTotal = registro.Planilla != null ? registro.Planilla.DeduccionesTotal : 0m,
                SalarioNetoTotal = registro.Planilla != null ? registro.Planilla.SalarioNetoTotal : 0m
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (periodo?.IdPlanilla is not long idPlanilla)
        {
            return periodo;
        }

        var detalles = await contexto.DetallesPlanilla
            .AsNoTracking()
            .Where(detalle => detalle.IdPlanilla == idPlanilla)
            .OrderBy(detalle => detalle.Colaborador!.PrimerApellido)
            .ThenBy(detalle => detalle.Colaborador!.Nombre)
            .Select(detalle => new
            {
                detalle.IdColaborador,
                CodigoColaborador = detalle.Colaborador != null ? detalle.Colaborador.CodigoColaborador : string.Empty,
                Nombre = detalle.Colaborador != null ? detalle.Colaborador.Nombre : string.Empty,
                PrimerApellido = detalle.Colaborador != null ? detalle.Colaborador.PrimerApellido : string.Empty,
                SegundoApellido = detalle.Colaborador != null ? detalle.Colaborador.SegundoApellido : null,
                Departamento = detalle.Colaborador != null && detalle.Colaborador.Departamento != null
                    ? detalle.Colaborador.Departamento.Nombre
                    : string.Empty,
                detalle.SalarioBase,
                detalle.SalarioProporcional,
                detalle.TotalHorasExtra,
                detalle.TotalBonos,
                detalle.TotalBeneficiosConfigurables,
                detalle.TotalAusencias,
                detalle.SalarioBruto,
                detalle.TotalDeducciones,
                detalle.SalarioNeto
            })
            .ToListAsync(cancellationToken);

        periodo.Detalles = detalles.Select(detalle => new DetallePlanillaResumen
        {
            IdColaborador = detalle.IdColaborador,
            CodigoColaborador = detalle.CodigoColaborador,
            NombreColaborador = ConstruirNombreCompleto(
                detalle.Nombre,
                detalle.PrimerApellido,
                detalle.SegundoApellido),
            Departamento = detalle.Departamento,
            SalarioBase = detalle.SalarioBase,
            SalarioProporcional = detalle.SalarioProporcional,
            TotalHorasExtra = detalle.TotalHorasExtra,
            TotalBonos = detalle.TotalBonos,
            TotalBeneficiosConfigurables = detalle.TotalBeneficiosConfigurables,
            TotalAusencias = detalle.TotalAusencias,
            SalarioBruto = detalle.SalarioBruto,
            TotalDeducciones = detalle.TotalDeducciones,
            SalarioNeto = detalle.SalarioNeto
        }).ToList();

        return periodo;
    }

    public async Task CalcularPlanillaAsync(
        long idPeriodoPlanilla,
        string? idUsuarioActual = null,
        CancellationToken cancellationToken = default)
    {
        await using var transaccion = await contexto.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        try
        {
            var periodo = await contexto.PeriodosPlanilla
                .Include(registro => registro.EstadoPlanilla)
                .FirstOrDefaultAsync(registro => registro.IdPeriodoPlanilla == idPeriodoPlanilla
                    && registro.EstadoRegistro == EstadoActivo, cancellationToken)
                ?? throw new InvalidOperationException("No se encontró el período de planilla solicitado.");

            if (!string.Equals(periodo.EstadoPlanilla?.Nombre, EstadosPlanilla.Borrador, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Solo se puede calcular un período en estado Borrador.");
            }

            if (await contexto.Planillas.AnyAsync(planilla =>
                planilla.IdPeriodoPlanilla == idPeriodoPlanilla,
                cancellationToken))
            {
                throw new InvalidOperationException("El período ya tiene una planilla generada.");
            }

            var estadoCalculada = await ObtenerEstadoAsync(EstadosPlanilla.Calculada, cancellationToken);
            var colaboradores = await ObtenerColaboradoresActivosAsync(cancellationToken);

            if (colaboradores.Count == 0)
            {
                throw new InvalidOperationException("No hay colaboradores laboralmente activos para calcular.");
            }

            var idsColaboradores = colaboradores.Select(colaborador => colaborador.IdColaborador).ToList();
            var contratos = await contexto.Contratos
                .AsNoTracking()
                .Where(contrato => idsColaboradores.Contains(contrato.IdColaborador)
                    && contrato.EstadoRegistro == EstadoActivo
                    && contrato.EstadoContrato == EstadoContratoActivo
                    && contrato.FechaInicio <= periodo.FechaInicio
                    && (!contrato.FechaFin.HasValue || contrato.FechaFin.Value >= periodo.FechaFin))
                .ToListAsync(cancellationToken);

            var incidencias = await contexto.IncidenciasPlanilla
                .AsNoTracking()
                .Include(incidencia => incidencia.TipoIncidenciaPlanilla)
                .Where(incidencia => incidencia.IdPeriodoPlanilla == idPeriodoPlanilla
                    && incidencia.EstadoRegistro == EstadoActivo)
                .ToListAsync(cancellationToken);

            var parametros = await contexto.ParametrosPlanilla
                .AsNoTracking()
                .Include(parametro => parametro.AsignacionesColaborador)
                .Include(parametro => parametro.AsignacionesPeriodo)
                .Where(parametro => parametro.EstadoRegistro == EstadoActivo
                    && parametro.Naturaleza != null
                    && (!parametro.FechaVigenciaInicio.HasValue
                        || parametro.FechaVigenciaInicio.Value <= periodo.FechaFin)
                    && (!parametro.FechaVigenciaFin.HasValue
                        || parametro.FechaVigenciaFin.Value >= periodo.FechaFin))
                .ToListAsync(cancellationToken);

            var ahora = DateTime.Now;
            var planilla = new Planilla
            {
                IdPeriodoPlanilla = periodo.IdPeriodoPlanilla,
                IdEstadoPlanilla = estadoCalculada.IdEstadoPlanilla,
                FechaCalculo = ahora,
                CostoPatronalEstimadoTotal = 0m,
                FechaCreacion = ahora,
                CreadoPor = LimpiarIdUsuario(idUsuarioActual),
                EstadoRegistro = EstadoActivo
            };

            foreach (var colaborador in colaboradores)
            {
                var contratosAplicables = contratos
                    .Where(contrato => contrato.IdColaborador == colaborador.IdColaborador)
                    .ToList();

                if (contratosAplicables.Count != 1)
                {
                    throw new InvalidOperationException(
                        $"El colaborador {colaborador.CodigoColaborador} debe tener exactamente un contrato activo que cubra todo el período.");
                }

                var contrato = contratosAplicables[0];
                if (!string.Equals(contrato.PeriodicidadPago, periodo.TipoPeriodo, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        $"La periodicidad del contrato del colaborador {colaborador.CodigoColaborador} no coincide con el período.");
                }

                var incidenciasColaborador = incidencias
                    .Where(incidencia => incidencia.IdColaborador == colaborador.IdColaborador)
                    .ToList();
                var montosIncidencias = ClasificarIncidencias(incidenciasColaborador, colaborador.CodigoColaborador);
                var parametrosAplicables = ConstruirParametrosAplicables(
                    parametros,
                    periodo.IdPeriodoPlanilla,
                    colaborador.IdColaborador);

                ResultadoCalculoPlanilla resultado;
                try
                {
                    resultado = CalculadoraPlanilla.Calcular(new DatosCalculoPlanilla(
                        contrato.SalarioBase,
                        periodo.TipoPeriodo,
                        montosIncidencias.HorasExtra,
                        montosIncidencias.Bonos,
                        montosIncidencias.Ausencias,
                        montosIncidencias.Deducciones,
                        parametrosAplicables));
                }
                catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
                {
                    throw new InvalidOperationException(
                        $"No se pudo calcular al colaborador {colaborador.CodigoColaborador}: {ex.Message}",
                        ex);
                }

                planilla.Detalles.Add(new DetallePlanilla
                {
                    IdColaborador = colaborador.IdColaborador,
                    SalarioBase = resultado.SalarioBase,
                    SalarioProporcional = resultado.SalarioProporcional,
                    TotalHorasExtra = resultado.TotalHorasExtra,
                    TotalBonos = resultado.TotalBonos,
                    TotalBeneficiosConfigurables = resultado.TotalBeneficiosConfigurables,
                    TotalAusencias = resultado.TotalAusencias,
                    SalarioBruto = resultado.SalarioBruto,
                    TotalDeducciones = resultado.TotalDeducciones,
                    SalarioNeto = resultado.SalarioNeto,
                    CostoPatronalEstimado = 0m,
                    FechaCreacion = ahora
                });
            }

            planilla.SalarioBrutoTotal = planilla.Detalles.Sum(detalle => detalle.SalarioBruto);
            planilla.DeduccionesTotal = planilla.Detalles.Sum(detalle => detalle.TotalDeducciones);
            planilla.SalarioNetoTotal = planilla.Detalles.Sum(detalle => detalle.SalarioNeto);

            periodo.IdEstadoPlanilla = estadoCalculada.IdEstadoPlanilla;
            periodo.FechaModificacion = ahora;
            periodo.ModificadoPor = LimpiarIdUsuario(idUsuarioActual);

            contexto.Planillas.Add(planilla);
            await contexto.SaveChangesAsync(cancellationToken);

            contexto.BitacoraAuditoria.Add(new BitacoraAuditoria
            {
                IdUsuario = LimpiarIdUsuario(idUsuarioActual),
                FechaHora = ahora,
                Accion = "CALCULAR_PLANILLA",
                Entidad = "Planilla",
                IdRegistro = planilla.IdPlanilla.ToString(),
                ValoresNuevos = JsonSerializer.Serialize(new
                {
                    planilla.IdPeriodoPlanilla,
                    Estado = EstadosPlanilla.Calculada,
                    Colaboradores = planilla.Detalles.Count,
                    planilla.SalarioBrutoTotal,
                    planilla.DeduccionesTotal,
                    planilla.SalarioNetoTotal
                }),
                Observacion = "Planilla calculada y período actualizado a estado Calculada."
            });

            await contexto.SaveChangesAsync(cancellationToken);
            await transaccion.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaccion.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    public Task AprobarPlanillaAsync(
        long idPeriodoPlanilla,
        string idUsuarioActual,
        CancellationToken cancellationToken = default)
    {
        return CambiarEstadoPlanillaAsync(
            idPeriodoPlanilla,
            idUsuarioActual,
            EstadosPlanilla.Aprobada,
            cancellationToken);
    }

    public Task CerrarPlanillaAsync(
        long idPeriodoPlanilla,
        string idUsuarioActual,
        CancellationToken cancellationToken = default)
    {
        return CambiarEstadoPlanillaAsync(
            idPeriodoPlanilla,
            idUsuarioActual,
            EstadosPlanilla.Cerrada,
            cancellationToken);
    }

    private async Task CambiarEstadoPlanillaAsync(
        long idPeriodoPlanilla,
        string idUsuarioActual,
        string estadoDestino,
        CancellationToken cancellationToken)
    {
        var idUsuario = LimpiarIdUsuarioObligatorio(idUsuarioActual);

        await using var transaccion = await contexto.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        try
        {
            var periodo = await contexto.PeriodosPlanilla
                .Include(registro => registro.EstadoPlanilla)
                .Include(registro => registro.Planilla)!
                    .ThenInclude(planilla => planilla!.EstadoPlanilla)
                .FirstOrDefaultAsync(registro => registro.IdPeriodoPlanilla == idPeriodoPlanilla
                    && registro.EstadoRegistro == EstadoActivo, cancellationToken)
                ?? throw new InvalidOperationException("No se encontró el período de planilla solicitado.");

            var planilla = periodo.Planilla
                ?? throw new InvalidOperationException("El período no tiene una planilla calculada.");

            if (string.Equals(estadoDestino, EstadosPlanilla.Aprobada, StringComparison.OrdinalIgnoreCase))
            {
                FlujoEstadosPlanilla.ValidarAprobacion(
                    periodo.EstadoPlanilla?.Nombre,
                    planilla.EstadoPlanilla?.Nombre);
            }
            else
            {
                FlujoEstadosPlanilla.ValidarCierre(
                    periodo.EstadoPlanilla?.Nombre,
                    planilla.EstadoPlanilla?.Nombre);
            }

            var estadoAnterior = planilla.EstadoPlanilla!.Nombre;
            var nuevoEstado = await ObtenerEstadoAsync(estadoDestino, cancellationToken);
            var ahora = DateTime.Now;

            periodo.IdEstadoPlanilla = nuevoEstado.IdEstadoPlanilla;
            periodo.FechaModificacion = ahora;
            periodo.ModificadoPor = idUsuario;

            planilla.IdEstadoPlanilla = nuevoEstado.IdEstadoPlanilla;
            planilla.FechaModificacion = ahora;
            planilla.ModificadoPor = idUsuario;

            var accion = "CERRAR_PLANILLA";
            var observacion = "Planilla y período actualizados a estado Cerrada.";

            if (string.Equals(estadoDestino, EstadosPlanilla.Aprobada, StringComparison.OrdinalIgnoreCase))
            {
                planilla.AprobadoPor = idUsuario;
                planilla.FechaAprobacion = ahora;
                accion = "APROBAR_PLANILLA";
                observacion = "Planilla aprobada y bloqueada para cambios posteriores.";
            }
            else
            {
                planilla.CerradoPor = idUsuario;
                planilla.FechaCierre = ahora;
            }

            contexto.BitacoraAuditoria.Add(new BitacoraAuditoria
            {
                IdUsuario = idUsuario,
                FechaHora = ahora,
                Accion = accion,
                Entidad = "Planilla",
                IdRegistro = planilla.IdPlanilla.ToString(),
                ValoresAnteriores = JsonSerializer.Serialize(new { Estado = estadoAnterior }),
                ValoresNuevos = JsonSerializer.Serialize(new
                {
                    Estado = estadoDestino,
                    planilla.FechaAprobacion,
                    planilla.FechaCierre
                }),
                Observacion = observacion
            });

            await contexto.SaveChangesAsync(cancellationToken);
            await transaccion.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaccion.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    private async Task<EstadoPlanilla> ObtenerEstadoAsync(
        string nombre,
        CancellationToken cancellationToken)
    {
        return await contexto.EstadosPlanilla
            .SingleOrDefaultAsync(estado => estado.Nombre == nombre
                && estado.EstadoRegistro == EstadoActivo, cancellationToken)
            ?? throw new InvalidOperationException($"No está configurado el estado de planilla {nombre}.");
    }

    private async Task<List<ColaboradorCalculo>> ObtenerColaboradoresActivosAsync(
        CancellationToken cancellationToken)
    {
        return await contexto.Colaboradores
            .AsNoTracking()
            .Where(colaborador => colaborador.EstadoRegistro == EstadoActivo
                && colaborador.EstadoLaboral != null
                && colaborador.EstadoLaboral.Nombre == EstadosLaborales.Activo)
            .OrderBy(colaborador => colaborador.CodigoColaborador)
            .Select(colaborador => new ColaboradorCalculo(
                colaborador.IdColaborador,
                colaborador.CodigoColaborador))
            .ToListAsync(cancellationToken);
    }

    private static MontosIncidencias ClasificarIncidencias(
        IReadOnlyCollection<IncidenciaPlanilla> incidencias,
        string codigoColaborador)
    {
        decimal horasExtra = 0;
        decimal bonos = 0;
        decimal ausencias = 0;
        decimal deducciones = 0;

        foreach (var incidencia in incidencias)
        {
            var nombre = incidencia.TipoIncidenciaPlanilla?.Nombre;
            if (string.Equals(nombre, TiposIncidenciaPlanilla.HoraExtra, StringComparison.OrdinalIgnoreCase))
            {
                horasExtra += incidencia.Monto;
            }
            else if (string.Equals(nombre, TiposIncidenciaPlanilla.Bono, StringComparison.OrdinalIgnoreCase))
            {
                bonos += incidencia.Monto;
            }
            else if (string.Equals(nombre, TiposIncidenciaPlanilla.AusenciaSinGoce, StringComparison.OrdinalIgnoreCase))
            {
                ausencias += incidencia.Monto;
            }
            else if (string.Equals(nombre, TiposIncidenciaPlanilla.Deduccion, StringComparison.OrdinalIgnoreCase))
            {
                deducciones += incidencia.Monto;
            }
            else
            {
                throw new InvalidOperationException(
                    $"La incidencia {incidencia.IdIncidenciaPlanilla} del colaborador {codigoColaborador} usa un tipo no reconocido.");
            }
        }

        return new MontosIncidencias(horasExtra, bonos, ausencias, deducciones);
    }

    private static IReadOnlyCollection<ParametroAplicablePlanilla> ConstruirParametrosAplicables(
        IReadOnlyCollection<ParametroPlanilla> parametros,
        long idPeriodoPlanilla,
        long idColaborador)
    {
        return parametros.Select(parametro => new ParametroAplicablePlanilla(
            parametro.Codigo,
            parametro.TipoParametro,
            parametro.Naturaleza,
            parametro.ValorDecimal,
            parametro.AsignacionesPeriodo.FirstOrDefault(asignacion =>
                asignacion.IdPeriodoPlanilla == idPeriodoPlanilla
                && asignacion.EstadoRegistro == EstadoActivo)?.ValorDecimalOverride,
            parametro.AsignacionesColaborador.FirstOrDefault(asignacion =>
                asignacion.IdColaborador == idColaborador
                && asignacion.EstadoRegistro == EstadoActivo)?.ValorDecimalOverride)).ToList();
    }

    private static string ConstruirNombreCompleto(string nombre, string primerApellido, string? segundoApellido)
    {
        return string.IsNullOrWhiteSpace(segundoApellido)
            ? $"{nombre} {primerApellido}"
            : $"{nombre} {primerApellido} {segundoApellido}";
    }

    private static string? LimpiarIdUsuario(string? idUsuario)
    {
        return string.IsNullOrWhiteSpace(idUsuario) ? null : idUsuario.Trim();
    }

    private static string LimpiarIdUsuarioObligatorio(string idUsuario)
    {
        return string.IsNullOrWhiteSpace(idUsuario)
            ? throw new ArgumentException("No se pudo identificar al usuario que ejecuta la acción.")
            : idUsuario.Trim();
    }

    private sealed record ColaboradorCalculo(long IdColaborador, string CodigoColaborador);

    private sealed record MontosIncidencias(
        decimal HorasExtra,
        decimal Bonos,
        decimal Ausencias,
        decimal Deducciones);

    private sealed record DatosPeriodoLimpios(
        string CodigoPeriodo,
        string Nombre,
        string TipoPeriodo,
        DateTime FechaInicio,
        DateTime FechaFin,
        string? Observaciones)
    {
        public static DatosPeriodoLimpios DesdeSolicitud(SolicitudPeriodoPlanilla solicitud)
        {
            if (solicitud.FechaInicio is null || solicitud.FechaFin is null)
            {
                throw new ArgumentException("Las fechas inicial y final son obligatorias.");
            }

            if (solicitud.FechaFin.Value.Date < solicitud.FechaInicio.Value.Date)
            {
                throw new ArgumentException("La fecha final no puede ser anterior a la fecha inicial.");
            }

            var tipoPeriodo = TiposPeriodoPlanilla.Permitidos.FirstOrDefault(tipo =>
                    string.Equals(tipo, solicitud.TipoPeriodo?.Trim(), StringComparison.OrdinalIgnoreCase))
                ?? throw new ArgumentException("El tipo de período debe ser Mensual o Quincenal.");

            var codigo = LimpiarObligatorio(solicitud.CodigoPeriodo, "El código del período es obligatorio.")
                .ToUpperInvariant();
            var nombre = LimpiarObligatorio(solicitud.Nombre, "El nombre del período es obligatorio.");
            var observaciones = LimpiarOpcional(solicitud.Observaciones);

            if (codigo.Length > 30)
            {
                throw new ArgumentException("El código no debe superar los 30 caracteres.");
            }

            if (nombre.Length > 150)
            {
                throw new ArgumentException("El nombre no debe superar los 150 caracteres.");
            }

            if (observaciones?.Length > 500)
            {
                throw new ArgumentException("Las observaciones no deben superar los 500 caracteres.");
            }

            return new DatosPeriodoLimpios(
                codigo,
                nombre,
                tipoPeriodo,
                solicitud.FechaInicio.Value.Date,
                solicitud.FechaFin.Value.Date,
                observaciones);
        }

        private static string LimpiarObligatorio(string valor, string mensaje)
        {
            if (string.IsNullOrWhiteSpace(valor))
            {
                throw new ArgumentException(mensaje);
            }

            return valor.Trim();
        }

        private static string? LimpiarOpcional(string? valor)
        {
            return string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
        }
    }
}
