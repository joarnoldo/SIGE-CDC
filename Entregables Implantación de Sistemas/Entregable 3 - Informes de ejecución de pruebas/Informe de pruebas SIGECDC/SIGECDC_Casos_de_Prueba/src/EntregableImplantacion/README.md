# Suite académica — Entregable 1 de SIGE-CDC

Cliente: Canales y Drenajes del Caribe S.R.L.

## Alcance y trazabilidad

Esta suite nueva implementa los 42 casos aprobados de QA02. Tiene 30 HU oficiales seleccionadas de 39: **76,92 % de cobertura funcional planificada**. Las cuatro pruebas AUTH son transversales y no aumentan el numerador. Esta cifra no mide cobertura de código ni demuestra que todos los criterios de aceptación hayan aprobado.

Se conservan 6 PUB, 4 AUTH, 6 RH, 8 PLA, 9 ACT, 6 FOR y 3 AUD. Cada caso tiene un único método xUnit, DisplayName con TC ID y Trait HU. No hay casos omitidos ni una dependencia del orden de ejecución.

## Estrategia de automatización

- Componentes Blazor reales y sus eventos: PUB-002, PUB-003 y PUB-004. El arnés despacha eventos del árbol renderizado y el formulario ejecuta su validación real.
- Sistema por HTTP local: páginas públicas, Identity, cookies, antiforgery, permisos por rutas y descargas de colillas/reportes. No usa navegador automatizado.
- Integración: servicios reales, EF Core, MySQL 8, almacenamiento temporal, generación y lectura de PDF/Excel; operaciones de RRHH, activos, planilla, Forecast y auditoría.
- Los casos funcionales se describen en QA02; su implementación elige la capa mínima suficiente. Estas pruebas no equivalen a UAT, validación visual completa ni autorización productiva.

El host de integración compone los componentes y endpoints originales en Kestrel, sobre 127.0.0.1 y un puerto temporal. No ejecuta el arranque operativo de Program, los seeders empresariales ni el bootstrap del Administrador. Mantiene Identity, autorización y antiforgery reales. La revisión de diseño de AUTH-004 no exige permisos financieros al Administrador ni resuelve PH-01.

## Infraestructura reutilizada

Se reutilizan el proyecto SIGECDC.Tests.csproj y las dependencias ya presentes: .NET 10, xUnit, ASP.NET Core, EF Core/MySQL, ClosedXML y PDFsharp. No se agregan paquetes ni otro proyecto.

Del código anterior de pruebas se reutiliza exclusivamente **Finanzas/Fin02BMySqlSandbox.cs**, soporte genérico para iniciar un mysqld nuevo, verificar su UUID y crear bases aisladas desde el SQL oficial. No se reutilizan pruebas, escenarios, datasets dorados, aserciones ni resultados previos. La selección académica procede de las HU y los 42 casos aprobados.

La colección inicia una sola instancia local y cada TC crea su propia base desde cero. No utiliza SIGECDC_QA_CONNECTION, conexiones del workspace, MonsterASP ni una base del cliente. El soporte académico reutiliza hasta diez conexiones por base sintética y libera únicamente ese pool al terminar el caso; así evita abrir miles de sockets para operaciones cortas. El fixture conserva las bases sintéticas y logs de su instancia temporal como evidencia; detiene únicamente su propio servidor al finalizar. Los archivos PDF de expediente/mantenimiento se crean en memoria, se almacenan en un directorio temporal exclusivo y se eliminan al terminar cada TC.

## Datos y oráculos numéricos

Las cuentas, colaboradores, expedientes, activos y proyectos son sintéticos. Los correos usan example.invalid. La contraseña visible del fixture pertenece exclusivamente a las cuentas temporales creadas por las pruebas.

El dataset financiero nuevo contiene dos personas con salarios de 240.000 y 360.000 por mes. Todas las tasas son de simulación: tres componentes obreros de 1 %, diez patronales de 1 %, riesgo de trabajo de 2 %, renta exenta y mínimo sintético de 100.000. **No representa legislación vigente ni datos empresariales.**

Oráculos calculados para este dataset:

| Magnitud | Persona 1 | Persona 2 | Total |
|---|---:|---:|---:|
| Salario bruto | 240.000 | 360.000 | 600.000 |
| Cuotas obreras | 7.200 | 10.800 | 18.000 |
| Neto | 232.800 | 349.200 | 582.000 |
| Carga patronal, incluido RT | 28.800 | 43.200 | 72.000 |
| Costo laboral | 268.800 | 403.200 | 672.000 |

PLA-004 añade un beneficio no salarial de 2.500 únicamente a la primera persona: neto individual 235.300 y total 584.500. FOR-003 y la alternativa de FOR-005 proyectan 5.000 no salariales por persona: neto total 592.000. El costo laboral permanece en 672.000: la especificación del proyecto lo define como bruto salarial más cargas patronales y distingue los ingresos no salariales del neto. FOR-004 utiliza dos borradores equivalentes: uno excluye a la segunda persona y proyecta 268.800; el otro la excluye y reincorpora antes de calcular, y proyecta 672.000. La diferencia de 403.200 demuestra su aporte. Las modificaciones ocurren en estado editable, conforme al caso aprobado.

Las URLs de los paquetes de simulación usan los hosts que admite la validación del producto y un recurso expresamente identificado como QA ficticio; no se consultan ni se presentan como publicaciones oficiales. Los paquetes contienen evidencia sintética y se preparan mediante los servicios reales de gobierno para satisfacer las precondiciones del cálculo, sin repetir la certificación técnica B–N.

El período financiero es enero de 2032 y la proyección febrero de 2032. Los preventivos se programan a 30 días del día de ejecución porque el producto valida que sean futuros. No hay esperas temporizadas para hacer aprobar reglas de negocio.

## Ejecución técnica

Desde SIGECDC:

```powershell
dotnet restore SIGECDC.slnx
dotnet build SIGECDC.slnx --no-restore -m:1 -p:UseSharedCompilation=false
dotnet test SIGECDC.Tests/SIGECDC.Tests.csproj --no-build --filter "FullyQualifiedName~SIGECDC.Tests.EntregableImplantacion" --logger "trx;LogFileName=validacion-tecnica.trx" --results-directory SIGECDC.Tests/EntregableImplantacion/Evidencia
```

MySQL Server 8 debe estar instalado localmente; el soporte genérico también admite la ruta explícita SIGECDC_QA_MYSQLD_PATH. La ausencia de MySQL es un error de infraestructura, nunca una omisión silenciosa. Las ejecuciones técnicas y sus incidencias se documentan separadamente en el informe de validación.

**Execution Date, Result y Comments de QA02 permanecen vacíos.** La ejecución técnica del desarrollo no se convierte automáticamente en ejecución formal del curso.

La evidencia y los conteos se reúnen en [ValidacionTecnica.md](ValidacionTecnica.md). La revisión de incidencias y PH-01 está en [Hallazgos.md](Hallazgos.md).

Si falla una expectativa funcional, se conserva el caso y se registra el hallazgo. Solo se corrigen errores del soporte académico; no se modifica el producto ni se altera el resultado esperado para obtener verde.

## Matriz TC ↔ HU ↔ archivo ↔ método

| TC ID | HU | Archivo | Método |
|---|---|---|---|
| PUB-001 | HU-INFO-001 | [PublicoAcademicoTests.cs](PublicoAcademicoTests.cs) | Pub001_ConsultarPaginasInstitucionalesSinSesion |
| PUB-002 | HU-INFO-002 | [PublicoAcademicoTests.cs](PublicoAcademicoTests.cs) | Pub002_FiltrarPortafolioYConsultarDetalle |
| PUB-003 | HU-INFO-003 | [PublicoAcademicoTests.cs](PublicoAcademicoTests.cs) | Pub003_EnviarConsultaContactoValida |
| PUB-004 | HU-INFO-003 | [PublicoAcademicoTests.cs](PublicoAcademicoTests.cs) | Pub004_RechazarContactoConObligatoriosVacios |
| PUB-005 | HU-INFO-004 | [PublicoAcademicoTests.cs](PublicoAcademicoTests.cs) | Pub005_CrearActualizarYPublicarContenido |
| PUB-006 | HU-INFO-007 | [PublicoAcademicoTests.cs](PublicoAcademicoTests.cs) | Pub006_PublicarYOcultarProyecto |
| AUTH-001 | Transversal | [IdentidadAcademicoTests.cs](IdentidadAcademicoTests.cs) | Auth001_IniciarSesionConCredencialesValidas |
| AUTH-002 | Transversal | [IdentidadAcademicoTests.cs](IdentidadAcademicoTests.cs) | Auth002_RechazarCredencialesInvalidas |
| AUTH-003 | Transversal | [IdentidadAcademicoTests.cs](IdentidadAcademicoTests.cs) | Auth003_CerrarSesionYBloquearReingresoDirecto |
| AUTH-004 | Transversal | [IdentidadAcademicoTests.cs](IdentidadAcademicoTests.cs) | Auth004_AutorizarFuncionesPropiasYDenegarOtroRol |
| RH-001 | HU-RH-001 | [RecursosHumanosAcademicoTests.cs](RecursosHumanosAcademicoTests.cs) | Rh001_RegistrarYConsultarColaborador |
| RH-002 | HU-RH-001 | [RecursosHumanosAcademicoTests.cs](RecursosHumanosAcademicoTests.cs) | Rh002_ActualizarDatosDeColaborador |
| RH-003 | HU-RH-001 | [RecursosHumanosAcademicoTests.cs](RecursosHumanosAcademicoTests.cs) | Rh003_DesactivarColaboradorSinEliminarHistorial |
| RH-004 | HU-RH-002 | [RecursosHumanosAcademicoTests.cs](RecursosHumanosAcademicoTests.cs) | Rh004_CrearPuestoDepartamentoYAsignarColaborador |
| RH-005 | HU-RH-003 | [RecursosHumanosAcademicoTests.cs](RecursosHumanosAcademicoTests.cs) | Rh005_AdjuntarDocumentoAlExpedienteCorrecto |
| RH-006 | HU-RH-003 | [RecursosHumanosAcademicoTests.cs](RecursosHumanosAcademicoTests.cs) | Rh006_RechazarArchivoQueNoEsPdf |
| PLA-001 | HU-RH-004 | [PlanillaAcademicoTests.cs](PlanillaAcademicoTests.cs) | Pla001_RegistrarHorasExtraAusenciaYBono |
| PLA-002 | HU-RH-005 | [PlanillaAcademicoTests.cs](PlanillaAcademicoTests.cs) | Pla002_CalcularPlanillaConTotalesEsperados |
| PLA-003 | HU-RH-005 | [PlanillaAcademicoTests.cs](PlanillaAcademicoTests.cs) | Pla003_RechazarCalculoIncompletoSinPersistenciaParcial |
| PLA-004 | HU-RH-006 | [PlanillaAcademicoTests.cs](PlanillaAcademicoTests.cs) | Pla004_ConfigurarYAsignarConceptoAUnColaborador |
| PLA-005 | HU-RH-007 | [PlanillaAcademicoTests.cs](PlanillaAcademicoTests.cs) | Pla005_ConsultarYDescargarColillaPropia |
| PLA-006 | HU-RH-007 | [PlanillaAcademicoTests.cs](PlanillaAcademicoTests.cs) | Pla006_DenegarColillaAjenaPorAccesoDirecto |
| PLA-007 | HU-RH-008 | [PlanillaAcademicoTests.cs](PlanillaAcademicoTests.cs) | Pla007_AprobarCerrarYRechazarCambios |
| PLA-008 | HU-RH-009 | [PlanillaAcademicoTests.cs](PlanillaAcademicoTests.cs) | Pla008_ExportarReporteConPeriodoYTotalesCorrectos |
| ACT-001 | HU-ACT-001 | [ActivosAcademicoTests.cs](ActivosAcademicoTests.cs) | Act001_RegistrarActivoDisponible |
| ACT-002 | HU-ACT-002 | [ActivosAcademicoTests.cs](ActivosAcademicoTests.cs) | Act002_ActualizarEstadoYUbicacion |
| ACT-003 | HU-ACT-003 | [ActivosAcademicoTests.cs](ActivosAcademicoTests.cs) | Act003_ConsultarDisponibilidadPorFechasYEstado |
| ACT-004 | HU-ACT-004 | [ActivosAcademicoTests.cs](ActivosAcademicoTests.cs) | Act004_CrearYActualizarProyecto |
| ACT-005 | HU-ACT-005 | [ActivosAcademicoTests.cs](ActivosAcademicoTests.cs) | Act005_AsignarActivoDisponibleAProyecto |
| ACT-006 | HU-ACT-005 | [ActivosAcademicoTests.cs](ActivosAcademicoTests.cs) | Act006_RechazarAsignacionSolapada |
| ACT-007 | HU-ACT-006 | [ActivosAcademicoTests.cs](ActivosAcademicoTests.cs) | Act007_DefinirMantenimientoPreventivo |
| ACT-008 | HU-ACT-007 | [ActivosAcademicoTests.cs](ActivosAcademicoTests.cs) | Act008_CompletarOrdenCorrectivaConEvidencia |
| ACT-009 | HU-ACT-008 | [ActivosAcademicoTests.cs](ActivosAcademicoTests.cs) | Act009_AcumularLecturasYConsultarTiempoFueraServicio |
| FOR-001 | HU-FOR-001 | [ForecastAcademicoTests.cs](ForecastAcademicoTests.cs) | For001_GenerarProyeccionFuturaConHistoricoAutorizado |
| FOR-002 | HU-FOR-001 | [ForecastAcademicoTests.cs](ForecastAcademicoTests.cs) | For002_RechazarFuenteHistoricaNoAprobada |
| FOR-003 | HU-FOR-002 | [ForecastAcademicoTests.cs](ForecastAcademicoTests.cs) | For003_AplicarParametroSinModificarHistorico |
| FOR-004 | HU-FOR-003 | [ForecastAcademicoTests.cs](ForecastAcademicoTests.cs) | For004_ActualizarTotalesAlExcluirYReincorporarColaborador |
| FOR-005 | HU-FOR-006 | [ForecastAcademicoTests.cs](ForecastAcademicoTests.cs) | For005_GuardarYCompararEscenariosCompatibles |
| FOR-006 | HU-FOR-007 | [ForecastAcademicoTests.cs](ForecastAcademicoTests.cs) | For006_CompararProyeccionConPlanillaRealCompatible |
| AUD-001 | HU-INFO-008 | [AuditoriaAcademicoTests.cs](AuditoriaAcademicoTests.cs) | Aud001_ConsultarHistorialAccesosYContactoSinEdicion |
| AUD-002 | HU-RH-010 | [AuditoriaAcademicoTests.cs](AuditoriaAcademicoTests.cs) | Aud002_ConsultarTrazabilidadDeRecursosHumanosYPlanilla |
| AUD-003 | HU-ACT-010 | [AuditoriaAcademicoTests.cs](AuditoriaAcademicoTests.cs) | Aud003_ConsultarTrazabilidadDeActivosProyectosYAsignaciones |

## Integridad del entregable

QA01 y QA02 conservan sus plantillas oficiales y cuentan con respaldo previo en “Respaldo Fase B”. No se modifican las HU, los documentos de contexto, el SQL, las capas del producto, la configuración del sitio ni el hosting. Codex se utiliza como apoyo de diseño, implementación y revisión; la aceptación académica y las decisiones de QA corresponden a las personas responsables.
