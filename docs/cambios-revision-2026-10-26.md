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


## Cuarta ronda: resumen, historial por mes, correcciones, errores globales y CORS
- **Resumen** (`GET /api/summary?month=`): total prestado, capital recuperado, interés cobrado, saldo por cobrar, préstamos vencidos. Con `month` se limitan "prestado" y "cobrado" a ese mes; los saldos y vencidos son siempre de hoy.
- **Historial por mes:** filtro `month=yyyy-MM` en `GET /api/loans` y nuevo `GET /api/payments` (por cliente, por mes, con o sin anulados).
- **Anular pagos y cancelar préstamos:** `POST /api/loans/{id}/payments/{paymentId}/void` y `POST /api/loans/{id}/cancel`. Nada se borra: el pago queda marcado (`IsVoided`) y deja de contar; el saldo se recalcula y un préstamo pagado vuelve a activo. Un préstamo solo se cancela si no tiene pagos vigentes.
- **Errores globales:** `ValidationExceptionHandler` convierte `ValidationException` en 400 en un solo lugar; se quitaron los `try/catch` de los controllers.
- **CORS:** política `Frontend` con los orígenes de `Cors:AllowedOrigins` (en desarrollo, `http://localhost:5173`).
- **Migración necesaria:** la tabla `payments` gana tres columnas (`IsVoided`, `VoidedAtUtc`, `VoidReason`).

```powershell
cd backend
dotnet build
dotnet test
dotnet ef migrations add PaymentVoiding --project ControlDeDinero.Infrastructure --startup-project ControlDeDinero.Api --context AppDbContext
# revisa el archivo generado: solo debe AGREGAR 3 columnas a payments
dotnet ef database update --project ControlDeDinero.Infrastructure --startup-project ControlDeDinero.Api --context AppDbContext
```

## Quinta ronda: cuentas de usuario, login y datos separados por usuario
- **Quién es dueño de qué:** `Client` gana `OwnerId` (el usuario). Los préstamos y pagos llegan al usuario a través de su cliente. Todas las consultas (clientes, préstamos, pagos, resumen) se filtran por el usuario que inició sesión; lo de otro usuario responde 404.
- **Login:** `POST /api/auth/login` devuelve un token JWT (dura 7 días). `GET /api/auth/me` sirve para comprobar que el token funciona.
- **Todo protegido por defecto:** una política global exige estar autenticado; solo el login lleva `[AllowAnonymous]`. Un controller nuevo nunca queda abierto por olvido.
- **Sin registro público:** las cuentas se crean desde la terminal (`create-user`), con la contraseña escrita oculta. La primera cuenta que se crea adopta los clientes que ya existían sin dueño.
- **Capas:** `Application/Auth` (`ICurrentUser`, `IAuthService`, DTOs), `Infrastructure/Identity` (`ApplicationUser`, `AuthService`, `JwtTokenGenerator`, `JwtOptions`, `UserProvisioner`), `Api/Auth/CurrentUser`, `Api/Commands/CreateUserCommand`, `Api/Controllers/AuthController`.
- **Base de datos:** `AppDbContext` ahora hereda de `IdentityDbContext`. Tablas nuevas: `users`, `roles`, `user_roles`, `user_claims`, `user_logins`, `role_claims`, `user_tokens`; y `clients.OwnerId` (opcional) con llave foránea a `users`.
- **Tests:** `OwnershipTests` (aislamiento entre dos usuarios); los fakes y los tests existentes se adaptaron al nuevo parámetro de dueño.

```powershell
cd C:\loan-management-system\backend

# 1) Paquetes nuevos
dotnet add ControlDeDinero.Infrastructure package Microsoft.AspNetCore.Identity.EntityFrameworkCore
dotnet add ControlDeDinero.Infrastructure package Microsoft.IdentityModel.JsonWebTokens
dotnet add ControlDeDinero.Api package Microsoft.AspNetCore.Authentication.JwtBearer

# 2) Clave secreta para firmar tokens (genera una aleatoria y guárdala solo en user-secrets)
$b = New-Object byte[] 48; [System.Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($b)
dotnet user-secrets set "Jwt:Key" ([Convert]::ToBase64String($b)) --project ControlDeDinero.Api

# 3) Compilar y probar
dotnet build
dotnet test

# 4) Migración (revisa el archivo generado antes de aplicarla)
dotnet ef migrations add AddUsersAndOwnership --project ControlDeDinero.Infrastructure --startup-project ControlDeDinero.Api --context AppDbContext
dotnet ef database update --project ControlDeDinero.Infrastructure --startup-project ControlDeDinero.Api --context AppDbContext

# 5) Crear tu cuenta (pide la contraseña, mínimo 10 caracteres)
dotnet run --project ControlDeDinero.Api -- create-user tucorreo@ejemplo.com
```

La migración debe CREAR las tablas de Identity y AGREGAR `OwnerId` (con índice y llave foránea) a `clients`. Si ves `DropTable` o `DropColumn` sobre tus tablas de siempre, no la apliques y avisa.

## Sexta ronda: cambiar y resetear la contraseña, y cerrar sesiones viejas
- **Cambiar la contraseña (desde la app):** `POST /api/auth/change-password` con `currentPassword` y `newPassword`. Si la actual está mal o la nueva es débil responde 400. Devuelve un token nuevo.
- **Resetear la contraseña (desde la terminal):** `dotnet run --project ControlDeDinero.Api -- reset-password <correo>`. No pide la contraseña anterior, quita el bloqueo por intentos fallidos y cierra todas las sesiones de esa cuenta.
- **Sesiones que se cierran solas:** el token ahora lleva el `SecurityStamp` del usuario (claim `stamp`). En cada petición, `TokenSessionValidator` lo compara con el de la base; Identity cambia ese sello cuando cambia la contraseña, así que los tokens viejos dejan de servir (401). Cuesta una consulta pequeña a `users` por petición.
- **Código:** `ConsoleInput` reúne la lectura oculta de contraseñas que usan `create-user` y `reset-password`. `AddDefaultTokenProviders()` en `Program.cs` es necesario para el reseteo.
- **Base de datos:** sin cambios, no hay migración.
- **Ojo:** los tokens emitidos antes de esta ronda no traen `stamp`, así que dejan de funcionar. Hay que volver a hacer login.

```powershell
cd C:\loan-management-system\backend
dotnet build
dotnet test
dotnet run --project ControlDeDinero.Api -- reset-password tucorreo@ejemplo.com
```

## Séptima ronda: frontend (React) con detalle de clientes, formularios, estadística y modo oscuro
- **Backend:** `SummaryDto` gana `ExpectedInterest` (interés pactado de los préstamos del mes: el neto, la ganancia) y `ExpectedTotal` (capital + interés: el bruto). Tests nuevos en `SummaryServiceTests`. Sin migración.
- **Frontend nuevo en `frontend/`** (React + TypeScript + Vite + Tailwind v4 + React Router), estilo inspirado en un prototipo de referencia, pensado para celular:
  - Login, Inicio (por cobrar, cobrado del mes, vencidos, a quién cobrar), Préstamos (Todos/Vencidos), Clientes, Estadística y Perfil.
  - Detalle de cliente (`/clientes/:id`): cuánto debe hoy, teléfono para llamar, préstamos agrupados en Vencidos, Activos, Pagados y Cancelados.
  - Detalle de préstamo (`/prestamos/:id`): fechas, capital, interés, total, barra de pagado y la lista de pagos (los anulados, tachados).
  - Formularios: nuevo cliente (avisa si el nombre ya existía) y nuevo préstamo (atajos de 1 a 4 quincenas y vista previa del interés).
  - Estadística por mes con flechas: prestado, interés pactado (neto), total a recibir (bruto), lo cobrado y lo pendiente de hoy.
  - Modo oscuro en Perfil: Claro, Oscuro o Automático (sigue al celular).
- **Ojo:** la vista previa del interés en el formulario repite la regla del 20% por quincena del backend solo para mostrarla; el valor que se guarda lo calcula el backend.

```powershell
cd C:\loan-management-system\backend
dotnet build
dotnet test
dotnet run --project ControlDeDinero.Api

# en otra terminal
cd C:\loan-management-system\frontend
npm run dev
```

## Octava ronda: registrar pagos desde la app, lista de activos y gráficas mensuales
- **Backend:** nuevo `GET /api/summary/monthly?months=6` (`SummaryService.GetMonthlyAsync`, `MonthlyPointDto`): por mes, lo prestado, el capital que sigue afuera, y el capital e interés cobrados. Sin préstamos cancelados ni pagos anulados. Tests nuevos en `SummaryServiceTests`. Sin migración.
- **Préstamos:** la lista muestra los activos (los vencidos, en rojo) con las pestañas Activos / Vencidos. Los pagados quedan en el historial de cada cliente.
- **Registrar pago** en el detalle del préstamo: monto, día (no puede ser futuro ni antes del préstamo) y notas. Botón "Pagó todo". No deja guardar más de lo que falta (el backend también lo rechaza). Al completar el total, el préstamo queda "Pagado".
- **Anular pago** en el detalle del préstamo, con confirmación. El pago queda tachado y deja de contar.
- **Estadística:** dos gráficas de los últimos 6 meses: "Dinero prestado" (lo que ya volvió y lo que sigue afuera) y "Dinero que entró" (capital e interés). Tocar una barra muestra sus números. Colores validados para daltonismo y contraste, en claro y oscuro.
- **Mensajes del backend** más comunes, traducidos al español en `api/client.ts`.

```powershell
cd C:\loan-management-system\backend
dotnet test
```

## Novena ronda: archivar préstamos (darlos por perdidos) y "Cobrar este mes"
- **Archivar:** nuevo estado `Archived` (dado por perdido). `Loan.Archive()` solo para préstamos activos, `Loan.Unarchive()` lo devuelve a activo. Un archivado no recibe pagos, no cuenta como vencido ni en lo por cobrar, y anular uno de sus pagos no lo desarchiva. Endpoints `POST /api/loans/{id}/archive` y `/unarchive`. Sin migración: el estado se guarda como texto.
- **Resumen:** `DueThisMonth` y `DueThisMonthLoans` (lo que vence de hoy a fin de mes, sin los vencidos), `ArchivedLoans` y `ArchivedBalance` (lo dado por perdido). La gráfica mensual separa `PrincipalWrittenOff`.
- **Tests:** 4 en `LoanTests`, 3 en `LoanServiceTests` y 1 en `SummaryServiceTests` (más aserciones nuevas en el de todo el tiempo).
- **Frontend:** Inicio muestra "Cobrar este mes" y la lista "Vencen este mes". Los vencidos van aparte (tarjeta negra y pestaña Vencidos). Préstamos tiene 3 pestañas: Activos (al día), Vencidos y Archivados. En un préstamo vencido aparece "Dar por perdido (archivar)", con confirmación; en uno archivado, "Desarchivar". La gráfica "Dinero prestado" suma la serie "Dado por perdido" y la Estadística muestra cuánto se dio por perdido.

## Décima ronda: aviso de sin conexión, límite de espera y estadística ordenada
- **Sin conexión:** `components/ConnectionBanner.tsx` muestra una franja roja arriba de todas las pantallas cuando el dispositivo pierde la red, y una verde ("Conexión recuperada") por 4 segundos al volver. `hooks/useOnlineStatus.ts` escucha los eventos `online`/`offline` del navegador.
- **Límite de espera:** cada petición se cancela a los 15 segundos (`AbortController` en `api/client.ts`); se puede cambiar con `VITE_API_TIMEOUT_MS` en `.env.development`. Mensajes distintos para: sin internet, servidor lento y servidor caído.
- **Estadística:** "Hoy" va en su propia sección, separada de "Detalle por mes". Al cambiar de mes se quedan los números anteriores (atenuados) hasta que llegan los nuevos (`useLoadFor`), así la pantalla no salta.

## Undécima ronda: corregir préstamos, editar clientes y gráficas en pestañas
- **Backend:** `Loan.UpdateTerms` corrige cliente, monto y fechas, y recalcula el interés y el total. Solo se puede si ningún pago cuenta, porque el reparto capital/interés de los pagos depende de esos montos; si hay pagos, primero se anulan. Las notas siempre se pueden cambiar. Queda un `AuditEvent` "LoanEdited" con el antes y el después. Nuevo `PUT /api/loans/{id}` (`UpdateLoanRequest`, `LoanService.UpdateAsync`). Tests: 4 en `LoanTests` y 3 en `LoanServiceTests`. Sin migración.
- **Frontend:**
  - "Corregir préstamo" (`/prestamos/:id/editar`): si el préstamo tiene pagos, deja cambiar solo las notas y explica por qué.
  - "Editar" cliente (`/clientes/:id/editar`): nombre, teléfono y notas, con el `PUT /api/clients/{id}` que ya existía.
  - Los formularios de préstamo y de cliente ahora son componentes compartidos (`components/LoanForm.tsx` y `components/ClientForm.tsx`), usados para crear y para editar.
  - Estadística: las gráficas van en pestañas ("Dinero prestado" / "Dinero que entró"), una a la vez.
