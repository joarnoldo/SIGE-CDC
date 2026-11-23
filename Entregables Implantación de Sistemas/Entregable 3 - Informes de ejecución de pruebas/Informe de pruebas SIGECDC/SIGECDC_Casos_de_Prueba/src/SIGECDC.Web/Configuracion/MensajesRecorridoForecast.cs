using SIGECDC.Domain.Planillas;

namespace SIGECDC.Web.Configuracion;

public static class MensajesRecorridoForecast
{
    public static string Para(ConfiguracionConceptoException error) => error.Codigo switch
    {
        "FORECAST_COMPARACION_NO_ELEGIBLE" => "Calcule y guarde un escenario financiero vigente antes de compararlo con Planilla real.",
        "FORECAST_NO_DISPONIBLE" => "El escenario solicitado no está disponible.",
        "REAL_COMPARABLE_AMBIGUO" => "Existe más de una Planilla elegible para el período. Revise las ejecuciones reales antes de comparar.",
        "FORECAST_SNAPSHOT_NO_PUBLICABLE" or "FORECAST_SNAPSHOT_INCOMPLETO" or "FORECAST_SNAPSHOT_NO_CONCILIA" => "El escenario no conserva un snapshot financiero completo y conciliado. No se puede publicar ni comparar.",
        "OVERRIDE_FORECAST_NO_DEMOSTRADO" => "Indique un concepto vigente, un valor explícito y la referencia de soporte del override.",
        "AUTORIZACION_FORECAST_NO_DEMOSTRADA" => "El concepto requiere autorización: indique la referencia que respalda el override.",
        "MODALIDAD_FORECAST_INCOMPATIBLE" => "La modalidad y la unidad no son compatibles con el concepto seleccionado.",
        "NORMATIVA_NO_ADMITE_OVERRIDE" => "Las tasas normativas no pueden modificarse mediante overrides de Forecast.",
        "PRECISION_OVERRIDE_INVALIDA" => "Revise el valor del override (máximo cuatro decimales) y la longitud de sus referencias.",
        "CONDICIONES_FORECAST_SOLAPADAS" => "Ya existe una condición que cubre esas fechas. Revise los segmentos registrados; no se admiten solapamientos.",
        "CONDICION_FORECAST_NO_DEMOSTRABLE" or "CONDICION_FORECAST_NO_RESOLUBLE" or "GAP_CONDICION_FORECAST" or "CONDICION_FORECAST_VIGENCIA_CAMBIO" or "CONDICION_PROYECTADA_MODIFICADA" => "Confirme las condiciones proyectadas de cada participante para todo el rango aplicable: salario, jornada, ocupación, fechas y soporte. Revise si hubo cambios posteriores a la acreditación.",
        "CONDICION_FORECAST_INVALIDA" or "CONDICION_FORECAST_SIN_ORIGEN" or "CONDICION_EXISTENTE_NO_DEMOSTRABLE" or "CONDICION_FORECAST_ORIGEN_INCOMPATIBLE" => "Revise el participante, período, origen, fechas y soporte de la condición proyectada. No se crean contratos ficticios.",
        "PARTICIPANTE_FORECAST_INVALIDO" => "Seleccione un participante incluido en este escenario.",
        "PERIODO_FORECAST_INVALIDO" or "PERIODO_FORECAST_FALTANTE" => "Seleccione un período del escenario y registre únicamente fechas dentro de su vigencia.",
        "COMPARACION_FUERA_DE_RANGO" or "COMPARACION_IMPORTE_NO_MATERIALIZADO" => "Los importes del snapshot no permiten una comparación válida. Revise su conciliación y el rango del escenario; no se mostrarán resultados parciales.",
        _ when error.Codigo.Contains("DISTRIBUCION") => "Complete la distribución por participante y período: proyectos o Sin asignar, con porcentajes que concilien al 100 %.",
        _ when error.Codigo.Contains("APLICABILIDAD") && !error.Codigo.Contains("RT") => "Acredite cada componente CCSS/LPT: regla aplicable, clave funcional y referencias de respaldo. No se presume una excepción.",
        _ => MensajesRecorridoPlanilla.Para(error)
    };
}
