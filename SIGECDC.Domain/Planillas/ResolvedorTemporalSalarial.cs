namespace SIGECDC.Domain.Planillas;

public static class ResolvedorTemporalSalarial
{
    public static ResultadoTemporalSalarial Resolver(PeriodoSalarial periodo,
        IReadOnlyList<DatosTemporalesColaborador> colaboradores, IReadOnlyList<JornadaTemporal> jornadas,
        IReadOnlyList<MinimoTemporal> minimos)
    {
        periodo.Validar();
        var rango = new VigenciaFinanciera(periodo.Inicio, periodo.Fin);
        if (colaboradores.GroupBy(c => c.IdColaborador).Any(g => g.Count() != 1))
            throw new ArgumentException("La población contiene colaboradores duplicados.");
        var poblacion = new List<PoblacionSalarialTemporal>();
        foreach (var persona in colaboradores.OrderBy(c => c.IdColaborador))
        {
            var bloqueos = persona.Bloqueos.ToList();
            var segmentos = new List<SegmentoSalarialTemporal>();
            void Bloquear(string codigo, DateTime fecha, long? contrato = null) => bloqueos.Add(new(codigo, fecha, contrato));
            bool Valida(VigenciaFinanciera vigencia, string codigo)
            {
                try { vigencia.Validar(); return true; }
                catch (ArgumentException) { Bloquear(codigo, periodo.Inicio); return false; }
            }
            if (!Valida(persona.Relacion, "RELACION_FECHAS_INVALIDAS"))
            { poblacion.Add(new(persona.IdColaborador, [], bloqueos)); continue; }
            var contratos = persona.Contratos.Where(c => Valida(c.Vigencia, "CONTRATO_FECHAS_INVALIDAS")
                && c.Vigencia.Solapa(rango)).ToArray();
            if (contratos.Any(c => !c.Vigencia.Solapa(persona.Relacion) || c.Vigencia.Inicio < persona.Relacion.Inicio))
                Bloquear("CONTRATO_FUERA_RELACION", periodo.Inicio);
            // Ningún filtro depende del EstadoLaboral actual, ni del nombre del puesto.
            if (!persona.Relacion.Solapa(rango) || contratos.Length == 0)
            {
                if (bloqueos.Count > 0) poblacion.Add(new(persona.IdColaborador, [], bloqueos));
                continue;
            }
            var ocupaciones = persona.Ocupaciones.Where(o => Valida(o.Vigencia, "OCUPACION_FECHAS_INVALIDAS")).ToArray();
            var codigosJornada = contratos.Select(c => c.CodigoJornada).Where(c => c is not null).ToHashSet();
            var jornadasPersona = jornadas.Where(j => codigosJornada.Contains(j.Codigo)
                && Valida(j.Vigencia, "JORNADA_FECHAS_INVALIDAS")).ToArray();
            var idsOcupacion = ocupaciones.Select(o => o.IdOcupacion).ToHashSet();
            var minimosPersona = minimos.Where(m => idsOcupacion.Contains(m.IdOcupacion)
                && Valida(m.Vigencia, "MINIMO_FECHAS_INVALIDAS")
                && Valida(m.VigenciaNormativa, "NORMATIVA_FECHAS_INVALIDAS")).ToArray();
            var fronteras = new List<DateTime>();
            void Agregar(VigenciaFinanciera vigencia)
            {
                fronteras.Add(vigencia.Inicio);
                if (vigencia.Fin is { } fin && fin < periodo.Fin) fronteras.Add(fin.AddDays(1));
            }
            Agregar(persona.Relacion);
            foreach (var c in contratos) Agregar(c.Vigencia);
            foreach (var o in ocupaciones) Agregar(o.Vigencia);
            foreach (var j in jornadasPersona) Agregar(j.Vigencia);
            foreach (var m in minimosPersona) { Agregar(m.Vigencia); Agregar(m.VigenciaNormativa); }

            foreach (var tramo in CalendarioSalarial.Dividir(rango, fronteras))
            {
                var fecha = tramo.Inicio;
                if (!persona.Relacion.Contiene(fecha)) continue;
                var aplicables = contratos.Where(c => c.Vigencia.Contiene(fecha)).ToArray();
                if (aplicables.Length == 0) continue; // Gap sin relación contractual: no se remunera.
                if (aplicables.Length > 1) { Bloquear("CONTRATOS_SOLAPADOS", fecha); continue; }
                var contrato = aplicables[0];
                var antes = bloqueos.Count;
                void Falta(string codigo) => Bloquear(codigo, fecha, contrato.IdContrato);
                if (string.IsNullOrWhiteSpace(contrato.ReferenciaRelacionOrdinaria)) Falta("RELACION_ORDINARIA_NO_ACREDITADA");
                if (!contrato.HistoriaSalarialVerificable) Falta("HISTORIA_SALARIAL_NO_DEMOSTRABLE");
                if (contrato.SalarioMensual <= 0) Falta("SALARIO_CONTRACTUAL_INVALIDO");
                T? Unico<T>(IEnumerable<T> valores, string codigo) where T : class
                {
                    var filas = valores.ToArray();
                    if (filas.Length == 1) return filas[0];
                    Falta(codigo + (filas.Length == 0 ? "_FALTANTE" : "_AMBIGUO")); return null;
                }
                var jornada = Unico(jornadasPersona.Where(j => j.Codigo == contrato.CodigoJornada && j.Vigencia.Contiene(fecha)
                    && (contrato.IdJornadaDemostrada is null || j.IdJornada == contrato.IdJornadaDemostrada)), "JORNADA");
                if (jornada is not null && (!jornada.Utilizable || jornada.HorasDia <= 0 || jornada.HorasSemana <= 0
                    || jornada.Tipo is not ("Diurna" or "Mixta" or "Nocturna"))) Falta("JORNADA_NO_UTILIZABLE");
                var ocupacion = Unico(ocupaciones.Where(o => o.Vigencia.Contiene(fecha)
                    && (o.IdContrato is null || o.IdContrato == contrato.IdContrato)), "OCUPACION_HISTORICA");
                if (ocupacion is not null && (ocupacion.IdOcupacion <= 0 || string.IsNullOrWhiteSpace(ocupacion.Codigo)
                    || string.IsNullOrWhiteSpace(ocupacion.ReferenciaSoporte))) Falta("OCUPACION_SIN_EVIDENCIA");
                MinimoTemporal? minimo = null;
                if (jornada is not null && ocupacion is not null)
                {
                    minimo = Unico(minimosPersona.Where(m => m.IdOcupacion == ocupacion.IdOcupacion
                        && m.Periodicidad is "Mensual" or "Diario" && (m.TipoJornada is null || m.TipoJornada == jornada.Tipo)
                        && m.Vigencia.Contiene(fecha)), "SALARIO_MINIMO");
                    if (minimo is not null)
                    {
                        if (!minimo.NormativaVerificada || !minimo.VigenciaNormativa.Contiene(tramo)) Falta("NORMATIVA_NO_UTILIZABLE");
                        if (minimo.Monto <= 0) Falta("SALARIO_MINIMO_INVALIDO");
                        // Comparación en la unidad de la fuente; §3.2 define VD = S / 30.
                        var salarioComparable = minimo.Periodicidad == "Diario"
                            ? contrato.SalarioMensual / CalendarioSalarial.UnidadesPorMes : contrato.SalarioMensual;
                        if (salarioComparable < minimo.Monto) Falta("SALARIO_BAJO_MINIMO");
                    }
                }
                if (antes == bloqueos.Count && jornada is not null && ocupacion is not null && minimo is not null)
                    segmentos.Add(new(persona.IdColaborador, fecha, tramo.Fin!.Value,
                        CalendarioSalarial.DiasBase30(fecha, tramo.Fin.Value), contrato.IdContrato,
                        contrato.SalarioMensual, jornada, ocupacion, minimo, contrato.ReferenciaRelacionOrdinaria!,
                        contrato.IdCondicionLaboralVersion, contrato.IdPuesto, contrato.IdDepartamento));
            }
            // No entregar segmentos parciales utilizables de una persona cuya resolución está bloqueada.
            poblacion.Add(new(persona.IdColaborador, bloqueos.Count == 0 ? segmentos : [], bloqueos.Distinct().ToArray()));
        }
        return new(poblacion);
    }
}
