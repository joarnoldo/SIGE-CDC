using System.ComponentModel.DataAnnotations;
using SIGECDC.Domain.Planillas;

namespace SIGECDC.Web.Configuracion;

// Nunca publica ex.Message ni detalles SQL/infraestructura. Los códigos son contratos de negocio del motor.
public static class MensajesRecorridoPlanilla
{
    public static string Para(Exception error)
    {
        if (error is UnauthorizedAccessException) return "Su cuenta no está autorizada para esta operación de Recursos Humanos.";
        if (error is ValidationException) return "Complete los campos obligatorios y revise sus valores antes de continuar.";
        if (error is ConfiguracionConceptoException c)
            return string.Join(" ", c.Codigo.Split(';').Select(Traducir).Distinct());
        if (error is ArgumentException) return "Revise los campos obligatorios, las fechas, el soporte y la precisión de los valores. No se admiten vigencias incompatibles.";
        if (error is InvalidOperationException)
            return "La operación no es compatible con el estado o la evidencia vigente. Actualice la pantalla, revise las vigencias y no sustituya una versión histórica ni una asociación ya confirmada.";
        return "No se pudo completar la operación. No reintente a ciegas: actualice la pantalla para comprobar su estado y consulte el diagnóstico privado si persiste.";
    }

    public static string Traducir(string codigo)
    {
        codigo = codigo.Trim();
        if (codigo.Contains("JORNADA")) return "Falta una jornada válida y vigente: configúrela y confirme la condición laboral del colaborador.";
        if (codigo.Contains("MINIMO") || codigo.Contains("OCUPACION")) return "Revise la ocupación y el salario mínimo vigente del colaborador; el salario no puede ser inferior al mínimo aplicable.";
        if (codigo.Contains("CONDICION") || codigo.Contains("HISTORIA") || codigo.Contains("TEMPORAL") || codigo.Contains("CONTRATO") || codigo.Contains("RELACION"))
            return "Revise el contrato y sus condiciones laborales confirmadas: salario, jornada, modalidad, fechas y soporte de la relación ordinaria.";
        if (codigo.Contains("AUTORIZACION")) return "La incidencia o costo requiere autorización explícita y su referencia de respaldo.";
        if (codigo.Contains("CONFIRMACION") || codigo.Contains("MODIFICADA_DESPUES")) return "Confirme o reconfirme la incidencia con evidencia; registrar el hecho no equivale a confirmarlo.";
        if (codigo.Contains("TIPO_DIA") || codigo.Contains("HE_ESPECIAL")) return "Seleccione el tipo de día de las horas extra. Feriados, descansos o jornadas especiales sin regla vigente no pueden calcularse.";
        if (codigo.Contains("CANTIDAD") || codigo.Contains("DISTRIBUCION_TEMPORAL")) return "Indique una cantidad positiva de horas o días; registre cada fecha por separado y no un monto calculado manualmente.";
        if (codigo.Contains("MONTO_MANUAL")) return "Este concepto lo calcula el motor; no admite un monto manual.";
        if (codigo == "MONTO_DOCUMENTADO_FALTANTE") return "Indique expresamente el monto documentado del hecho. Un campo vacío no equivale a cero.";
        if (codigo.Contains("BASE_PORCENTUAL") || codigo.Contains("SEGMENTO")) return "Confirme la base salarial del tramo vigente en la fecha del hecho para aplicar el parámetro porcentual.";
        if (codigo.Contains("NORMATIVA_NO_ADMITE")) return "Las tasas y parámetros normativos no se editan ni sustituyen desde los parámetros internos de Planilla.";
        if (codigo.Contains("OVERRIDE_EXISTENTE")) return "Ya existe una asignación para ese destino. La evidencia se conserva; use una nueva versión con vigencia compatible.";
        if (codigo.Contains("PARAMETRO") || codigo.Contains("OVERRIDE") || codigo.Contains("VIGENCIA_INTERNA")) return "Seleccione un parámetro interno gobernado, compatible con el concepto, su unidad y la vigencia del hecho. Revise sus asignaciones.";
        if (codigo.Contains("CONCEPTO") || codigo.Contains("CLASIFICACION") || codigo.Contains("TIPO_INCIDENCIA") || codigo.Contains("CLASE_HECHO") || codigo.Contains("BANDERAS"))
            return "Falta un concepto clasificado y vigente para esta operación. Seleccione una versión gobernada; un tipo genérico legado no basta.";
        if (codigo.Contains("RT") || codigo.Contains("RIESGO") || codigo.Contains("POLIZA")) return "Falta demostrar la póliza de Riesgos del Trabajo, su asociación al contrato o su modalidad y vigencia. Revise la configuración RT.";
        if (codigo.Contains("CREDITO")) return "Revise la acreditación y vigencia del crédito fiscal del colaborador; no se puede inferir un crédito sin soporte.";
        if (codigo.Contains("CUOTA") || codigo.Contains("BMC") || codigo.Contains("APLICABILIDAD")) return "Complete la aplicabilidad de las cuotas por colaborador, con regla, vigencia y respaldo; no se infieren excepciones ni mínimos.";
        if (codigo.Contains("NORMATIV") || codigo.Contains("RENTA") || codigo.Contains("TASA")) return "Falta normativa activa, revisada y vigente para el período. Su carga y revisión no se sustituyen por parámetros internos.";
        if (codigo.Contains("NETO_NEGATIVO")) return "Las deducciones producen un neto negativo. Revise las incidencias y sus autorizaciones antes de aprobar.";
        if (codigo.Contains("TRANSICION") || codigo.Contains("EJECUCION") || codigo.Contains("PERIODO_BLOQUEADO")) return "El estado o la versión cambió. Actualice la pantalla; solo se reabre una Calculada y solo se cierra una Aprobada vigente.";
        if (codigo.Contains("ACUMULADO") || codigo.Contains("PREVIO")) return "Seleccione el snapshot vigente del primer corte del mismo mes; no se reconstruye el acumulado desde totales.";
        if (codigo.Contains("PERIODO") || codigo.Contains("FUERA")) return "Revise el período mensual o quincenal y la fecha del hecho: deben corresponder al calendario y a la relación laboral.";
        if (codigo.Contains("POBLACION")) return "No hay colaboradores con relación remunerable demostrada en este período. Revise contratos y fechas.";
        if (codigo.Contains("CONCILIA")) return "El snapshot no concilia. No se publicó ni autorizó el resultado; revise el diagnóstico privado antes de continuar.";
        if (codigo.Contains("UNIDAD") || codigo.Contains("BASE")) return "La unidad o base no es compatible con el concepto seleccionado. Use la modalidad indicada por su versión gobernada.";
        return "Existe una incompatibilidad en la evidencia financiera. Revise las condiciones, conceptos, normativa e incidencias del período; el cálculo permanece bloqueado.";
    }
}
