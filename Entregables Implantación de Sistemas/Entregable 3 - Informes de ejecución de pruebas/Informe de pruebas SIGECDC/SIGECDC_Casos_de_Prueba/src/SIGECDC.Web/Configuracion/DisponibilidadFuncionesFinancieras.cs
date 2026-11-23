namespace SIGECDC.Web.Configuracion;

public sealed record DisponibilidadFuncionesFinancieras(
    bool PlanillaDemostrativaHabilitada,
    bool ForecastExperimentalHabilitado,
    bool ModoDemoAcademicaHabilitado = false)
{
    public const string SeccionConfiguracion = "FuncionesFinancierasExperimentales";

    public static DisponibilidadFuncionesFinancieras Desde(
        IHostEnvironment ambiente,
        IConfiguration configuracion)
    {
        ArgumentNullException.ThrowIfNull(ambiente);
        ArgumentNullException.ThrowIfNull(configuracion);

        var esAmbienteTecnico = ambiente.IsDevelopment() || ambiente.IsEnvironment("QA");
        var modoDemoAcademica = ambiente.IsProduction()
            && configuracion.GetValue<bool>("ModoDemoAcademica:Habilitado");
        var permiteFuncionesFinancieras = esAmbienteTecnico || modoDemoAcademica;

        return new DisponibilidadFuncionesFinancieras(
            permiteFuncionesFinancieras && configuracion.GetValue<bool>(
                $"{SeccionConfiguracion}:PlanillaDemostrativaHabilitada"),
            permiteFuncionesFinancieras && configuracion.GetValue<bool>(
                $"{SeccionConfiguracion}:ForecastExperimentalHabilitado"),
            modoDemoAcademica);
    }
}
