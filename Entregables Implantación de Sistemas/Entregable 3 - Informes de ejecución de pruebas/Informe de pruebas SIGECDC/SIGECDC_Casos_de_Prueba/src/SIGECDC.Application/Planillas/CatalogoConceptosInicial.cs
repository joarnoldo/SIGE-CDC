using SIGECDC.Domain.Planillas;

namespace SIGECDC.Application.Planillas;

// PLN-06/09/12 y SQL-FIN-01. No importa cifras normativas ni clasifica conceptos de la empresa.
public static class CatalogoConceptosInicial
{
    public static IReadOnlyList<ConceptoCarga> Para(VigenciaFinanciera vigencia) =>
    [
        new("SALARIO_ORDINARIO", "Salario ordinario", vigencia,
            new("IngresoSalarial", true, true, true, false, "SALARIO_DEVENGADO", "Monto", false, false), "PLN-01/03/04/12"),
        new("HORA_EXTRA_ORDINARIA", "Hora extra ordinaria", vigencia,
            new("IngresoSalarial", true, true, true, false, "HORA_ORDINARIA", "Hora", false, true), "PLN-06/07/12", "Hora extra"),
        // Ajuste del devengo salarial, no una deducción posterior del neto. La cantidad/unidad y signo se resuelven en F1-FIN-02F.
        new("AUSENCIA_SIN_GOCE", "Ausencia sin goce", vigencia,
            new("IngresoSalarial", true, true, true, false, "REDUCCION_DEVENGO", "Cantidad", false, true), "PLN-09/10; PLA-GD-04/06", "Ausencia sin goce"),
        new("BONO", "Bono", vigencia, null, "PLN-08: clasificación empresarial pendiente", "Bono"),
        new("DEDUCCION_INTERNA", "Deducción interna", vigencia, null, "PLN-11/12: clasificación y autorización empresarial pendientes", "Deducción"),
        new("BENEFICIO_INTERNO", "Beneficio interno", vigencia, null, "PLN-12: clasificación empresarial pendiente"),
        Cuota("SEM_TRABAJADOR", "SEM trabajador", "BASE_SEM", false, vigencia),
        Cuota("IVM_TRABAJADOR", "IVM trabajador", "BASE_IVM", false, vigencia),
        Cuota("LPT_BPOP_TRABAJADOR", "LPT Banco Popular trabajador", "REMUNERACION_CONTRIBUTIVA", false, vigencia),
        Cuota("SEM_PATRONO", "SEM patrono", "BASE_SEM", true, vigencia),
        Cuota("IVM_PATRONO", "IVM patrono", "BASE_IVM", true, vigencia),
        Cuota("BPOP_PATRONO", "Banco Popular patrono", "REMUNERACION_CONTRIBUTIVA", true, vigencia),
        Cuota("ASIGNACIONES_FAMILIARES", "Asignaciones familiares", "REMUNERACION_CONTRIBUTIVA", true, vigencia),
        Cuota("IMAS", "IMAS", "REMUNERACION_CONTRIBUTIVA", true, vigencia),
        Cuota("INA", "INA", "REMUNERACION_CONTRIBUTIVA", true, vigencia),
        Cuota("LPT_BPOP_PATRONO", "LPT Banco Popular patrono", "REMUNERACION_CONTRIBUTIVA", true, vigencia),
        Cuota("LPT_FCL", "LPT Fondo de Capitalización Laboral", "REMUNERACION_CONTRIBUTIVA", true, vigencia),
        Cuota("LPT_OPC", "LPT pensiones complementarias", "REMUNERACION_CONTRIBUTIVA", true, vigencia),
        Cuota("LPT_INS", "Aporte INS de la LPT", "REMUNERACION_CONTRIBUTIVA", true, vigencia),
        new("IMPUESTO_SALARIO", "Impuesto sobre salarios", vigencia,
            new("DeduccionLegal", false, false, false, false, "RENTA_SALARIAL", "Monto", false, false), "PLN-20/21"),
        new("RIESGO_TRABAJO", "Riesgos del Trabajo", vigencia,
            new("CargaPatronal", false, false, false, true, "BASE_RT_POLIZA", "Porcentaje", false, false), "PLN-22/23")
    ];

    private static ConceptoCarga Cuota(string codigo, string nombre, string baseAplicable, bool patronal, VigenciaFinanciera vigencia)
        => new(codigo, nombre, vigencia, new(patronal ? "CargaPatronal" : "DeduccionLegal",
            false, false, false, patronal, baseAplicable, "Porcentaje", false, false), "PLN-13/18/19: componente individual, sin cuota agregada");
}
