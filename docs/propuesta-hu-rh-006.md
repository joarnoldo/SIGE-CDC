# Estabilización preparada — HU-RH-006

## Estado

El código y el DDL separado de esta estabilización fueron preparados el 2026-07-12. El DDL **no ha sido aplicado** a MySQL y el script SQL oficial no fue modificado.

Script para revisión y ejecución manual después de respaldo:

- `database/changes/20260712_HU_RH_006_Estabilizacion.sql`

## Brecha confirmada

`ParametroPlanilla` permite definir valores globales y vigencias, pero no puede asignar una deducción o beneficio a un colaborador o período específico, como exige HU-RH-006.

## Cambio de base de datos preparado

`ParametroPlanilla` incorpora una naturaleza opcional `Deduccion` o `Beneficio`. Los parámetros de referencia pueden conservar la naturaleza nula y no participan como concepto salarial.

Se recomiendan dos tablas de relación para evitar columnas nulas ambiguas y conservar restricciones únicas simples:

### `ParametroPlanillaColaborador`

- `IdParametroPlanillaColaborador BIGINT` como llave primaria.
- `IdParametroPlanilla INT` como llave foránea.
- `IdColaborador BIGINT` como llave foránea.
- `ValorDecimalOverride DECIMAL(18,4) NULL`.
- `ValorTextoOverride VARCHAR(255) NULL`.
- Campos de creación, modificación, usuario y `EstadoRegistro` siguiendo las convenciones actuales.
- Restricción única por parámetro y colaborador.

### `ParametroPlanillaPeriodo`

- `IdParametroPlanillaPeriodo BIGINT` como llave primaria.
- `IdParametroPlanilla INT` como llave foránea.
- `IdPeriodoPlanilla BIGINT` como llave foránea.
- `ValorDecimalOverride DECIMAL(18,4) NULL`.
- `ValorTextoOverride VARCHAR(255) NULL`.
- Campos de creación, modificación, usuario y `EstadoRegistro` siguiendo las convenciones actuales.
- Restricción única por parámetro y período.

## Impacto en código

- Domain: entidades para ambas asignaciones.
- Application: DTOs, solicitudes e interfaces de consulta/asignación.
- Persistence: mapeos en `ApplicationDbContext` y servicios EF Core.
- Web: selección de colaborador o período dentro de parámetros de planilla.
- Cálculo futuro: precedencia documentada de valor asignado sobre valor global.

## Decisiones funcionales fijadas

- Precedencia: colaborador, período y finalmente valor global.
- Un porcentaje se registra en formato humano: `10` representa `10%`.
- Solo parámetros `Porcentaje` o `Monto` pueden tener naturaleza de deducción o beneficio.
- Los overrides son opcionales; cuando no existen, la asignación utiliza el valor global.
- Desactivar un parámetro también desactiva sus asignaciones vigentes.

## Riesgos

- Duplicar asignaciones si no se aplican restricciones únicas.
- Aplicar simultáneamente un valor global, uno por período y uno por colaborador sin una precedencia definida.
- Afectar cálculos de planilla que aún no forman parte del cierre de Sprint 2.

## Orden para habilitar la funcionalidad en desarrollo

1. Respaldar `SIGE_CDC_DB`.
2. Revisar y aplicar manualmente el DDL separado en MySQL Workbench.
3. Ejecutar sus consultas de validación posterior.
4. Iniciar la aplicación y validar asignaciones, reactivación, duplicados y permisos.
5. Iniciar `HU-RH-005` únicamente cuando esta estabilización esté validada.
