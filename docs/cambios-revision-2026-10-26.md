# Cambios de la revisión (2026-10-26)

Resumen de lo que se modificó en el repositorio. Una copia del estado original quedó fuera de la carpeta del proyecto.

## Estructura
- Los 5 proyectos y la solución se movieron a `backend/`. Se crearon `frontend/` y `docs/requirements/` (vacías, con `.gitkeep`).
- Nombres unificados: todo es `ControlDeDinero.*` (antes mezclaba `ControlDeDinero` y `ControlDelDinero`). El proyecto de pruebas pasó de `ControlDelDinero.Test` a `ControlDeDinero.Tests`.
- `persistence/` pasó a `Persistence/`.
- Eliminados: `WeatherForecast`, su controller, el archivo `.http`, los dos `Class1.cs` y `UnitTest1.cs`.

## Dominio
- `LoanStatus` e `InstallmentStatus` (enums) en lugar de strings.
- `Loan` con setters privados. Se crea con `Loan.Create(...)` (calcula interés fijo y total) y se paga con `loan.RegisterPayment(...)` (reparto proporcional capital/interés, el último pago absorbe el redondeo, valida sobrepago y estado, deja registro en `AuditEvents`).
- `AuditEvent.CreatedAt` pasó a `CreatedAtUtc`.

## Persistencia (`AppDbContext`)
- Todas las entidades tienen `DbSet` y configuración. Tablas en snake_case: `clients`, `loans`, `installments`, `payments`, `payment_allocations`, `audit_events`.
- Todo `decimal` es `decimal(18,2)` por convención (`InterestRate` es `decimal(9,4)`).
- Fechas: conversión para leerlas siempre como UTC.
- Longitudes máximas, índices y un índice único `(LoanId, Number)` en `installments`.
- `Status` se guarda como texto (`varchar(20)`).

## Tests
- `ControlDeDinero.Tests/LoanTests.cs`: interés fijo (₡50,000 → ₡57,500), reparto proporcional, pago final con redondeo, sobrepago, pago a préstamo cerrado, auditoría.

## Documentación
- README: PostgreSQL → MySQL, estructura real, estado honesto del proyecto, caracteres corregidos, roadmap actualizado.
- Guías: rutas desde `backend/`, nombres de tablas nuevos, typo corregido.

## Pendiente que debes hacer tú (no había `dotnet` disponible para compilar)
```powershell
cd backend
dotnet build
dotnet test
dotnet ef migrations add DomainModelRefactor --project ControlDeDinero.Infrastructure --startup-project ControlDeDinero.Api --context AppDbContext
# revisar el archivo generado en Migrations/ y luego:
dotnet ef database update --project ControlDeDinero.Infrastructure --startup-project ControlDeDinero.Api --context AppDbContext
```
Si ya tienes filas de prueba en `loans` con `Status = 'Pending'`, bórralas o conviértelas a `Active` antes de aplicar la migración (el enum ya no tiene `Pending` para préstamos).


## Segunda ronda: interés de 20% por quincena
- `Loan.Create` ahora recibe la fecha límite (`dueDate`, obligatoria). Las quincenas se calculan: 15 días cada una, redondeando hacia arriba, mínimo 1 (`Loan.CountQuincenas`). Interés simple de 20% del capital por quincena. Ejemplo: 50,000 del 1 al 30 = 2 quincenas = 70,000.
- `InterestRate` pasó a `InterestRatePerQuincena` (por defecto 20). Columna nueva `Quincenas`. `DueDate` ya no admite nulos.
- Tests reescritos con el ejemplo de la regla del papá y los bordes de 14, 15, 16 y 30 días.
- README actualizado (ya no dice "15% único").

Supuestos pendientes de confirmar con el papá: pagar antes no reduce el interés; pasarse de la fecha no suma quincenas automáticamente.

### Migración que debes aplicar
```powershell
cd backend
dotnet build
dotnet test
dotnet ef migrations add QuincenaInterest --project ControlDeDinero.Infrastructure --startup-project ControlDeDinero.Api --context AppDbContext
```
Revisa el archivo generado (debe renombrar `InterestRate` a `InterestRatePerQuincena`, agregar `Quincenas` y volver `DueDate` NOT NULL) y luego `dotnet ef database update ...`.
Si `loans` tiene filas de prueba, antes de aplicar ejecuta en DBeaver:
```sql
UPDATE loans SET DueDate = DATE_ADD(LoanDate, INTERVAL 15 DAY) WHERE DueDate IS NULL;
```
y después de aplicar: `UPDATE loans SET Quincenas = 1 WHERE Quincenas = 0;`


## Tercera ronda: API de clientes, préstamos y pagos
- Clientes: crear, listar/buscar, ver, editar, desactivar y reactivar (`/api/clients`). Probado a mano con Postman.
- Préstamos y pagos: `/api/loans` (crear, listar con filtros, ver con pagos, registrar pago). Sin migración: la base no cambia.
- "Hoy" se calcula con la fecha de Costa Rica (`ZonedTimeProvider`), para que un préstamo que vence hoy no aparezca vencido a las 7 p. m.
- Los hijos `Payment`, `PaymentAllocation` y `AuditEvent` ya no se asignan su propio `Id`: lo genera EF al guardar. Si se les pone un Id al crearlos y se agregan a un préstamo que ya está cargado, EF los toma por filas existentes y falla al guardar (UPDATE en vez de INSERT).
- `Loan.RegisterPayment` rechaza fechas de pago anteriores a la fecha del préstamo.
- Tests: `LoanServiceTests`, dobles compartidos en `Fakes.cs`.
