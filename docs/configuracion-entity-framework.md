# Guía técnica para instalar Entity Framework Core con MySQL

## Objetivo

Documentar cómo se configuró la capa de persistencia de **Control de Dinero** utilizando:

- C# y .NET 10
- Entity Framework Core 10
- MySQL Community Server 8.4
- Migraciones Code First
- ASP.NET Core User Secrets

Esta guía está dirigida a desarrolladores que quieren comprender o reproducir la configuración desde cero.

## 1. Arquitectura del proyecto

La solución utiliza cuatro capas principales:

| Proyecto | Responsabilidad |
|---|---|
| `Domain` | Entidades y reglas del negocio |
| `Application` | Casos de uso |
| `Infrastructure` | Persistencia y servicios externos |
| `Api` | Entrada HTTP y configuración |
 
También existe un proyecto de pruebas.

Las entidades viven en `Domain`. La implementación de Entity Framework Core se encuentra en `Infrastructure`. El proyecto `Api` configura las dependencias al arrancar.

## 2. Localizar los proyectos

Desde la carpeta `backend` (donde está `ControlDeDinero.slnx`):

```powershell
$api = (Get-ChildItem -Recurse -Filter "*Api.csproj" |
    Select-Object -First 1).FullName

$infra = (Get-ChildItem -Recurse -Filter "*Infrastructure.csproj" |
    Select-Object -First 1).FullName
```

Verificar las rutas:

```powershell
Write-Host "API: $api"
Write-Host "Infrastructure: $infra"
```

No asumir que los nombres de las carpetas son idénticos a los nombres de los proyectos.

## 3. Instalar Entity Framework Core

En nuestra configuración utilizamos las siguientes versiones:

| Paquete | Versión | Proyecto |
|---|---|---|
| `MySql.EntityFrameworkCore` | 10.0.9 | Infrastructure |
| `Microsoft.EntityFrameworkCore.Tools` | 10.0.9 | Infrastructure |
| `Microsoft.EntityFrameworkCore.Design` | 10.0.9 | Api |
| `dotnet-ef` | 10.0.9 | Herramienta CLI |

Instalar los paquetes:

```powershell
dotnet add "$infra" package MySql.EntityFrameworkCore --version 10.0.9

dotnet add "$infra" package Microsoft.EntityFrameworkCore.Tools --version 10.0.9

dotnet add "$api" package Microsoft.EntityFrameworkCore.Design --version 10.0.9
```

Instalar la herramienta de línea de comandos:

```powershell
dotnet tool install --global dotnet-ef --version 10.0.9
```

Verificar:

```powershell
dotnet ef --version
dotnet build
```

**Importante:** estos comandos documentan la instalación inicial. No deben ejecutarse innecesariamente después de clonar un repositorio cuyos `.csproj` ya contienen las dependencias.

## 4. Preparar MySQL

Crear la base de datos:

```sql
CREATE DATABASE IF NOT EXISTS control_de_dinero
CHARACTER SET utf8mb4;
```

Crear el usuario:

```sql
CREATE USER 'controldinero_app'@'localhost'
IDENTIFIED WITH caching_sha2_password
BY 'CAMBIAR_POR_CONTRASEÑA_SEGURA';

GRANT SELECT, INSERT, UPDATE, DELETE,
      CREATE, ALTER, DROP, INDEX, REFERENCES
ON control_de_dinero.*
TO 'controldinero_app'@'localhost';
```

El método `caching_sha2_password` evita problemas con el antiguo sistema de autenticación de MySQL.

## 5. Crear y configurar AppDbContext

En el proyecto Infrastructure, crear una clase `AppDbContext` que herede de `DbContext`.

Ejemplo mínimo basado en las primeras entidades:

```csharp
using Microsoft.EntityFrameworkCore;
using ControlDeDinero.Domain;

namespace ControlDeDinero.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(
        DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Client> Clients => Set<Client>();
    public DbSet<Loan> Loans => Set<Loan>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Client>(entity =>
        {
            entity.ToTable("clients");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(x => x.Phone)
                .HasMaxLength(30);
        });

        modelBuilder.Entity<Loan>(entity =>
        {
            entity.ToTable("loans");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.PrincipalAmount)
                .HasPrecision(18, 2);

            entity.Property(x => x.InterestRate)
                .HasPrecision(9, 4);

            entity.HasOne(x => x.Client)
                .WithMany(x => x.Loans)
                .HasForeignKey(x => x.ClientId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Protección inicial de relaciones financieras.
        foreach (var foreignKey in modelBuilder.Model
            .GetEntityTypes()
            .SelectMany(entity => entity.GetForeignKeys()))
        {
            foreignKey.DeleteBehavior = DeleteBehavior.Restrict;
        }
    }
}
```

Este código es un ejemplo mínimo de las primeras entidades. El `AppDbContext` actual (`backend/ControlDeDinero.Infrastructure/Persistence/AppDbContext.cs`) ya configura todas las entidades, precisión decimal por convención, fechas UTC y eliminación restringida; consúltelo como referencia. Las propiedades y espacios de nombres deben coincidir con las entidades reales.

El diseño definitivo debería configurar las relaciones explícitamente y, de ser necesario, extraerlas a clases `IEntityTypeConfiguration<T>`.

## 6. Registrar AppDbContext en Program.cs

En el proyecto API, configurar Entity Framework Core con MySQL:

```csharp
using ControlDeDinero.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var connectionString = builder.Configuration
    .GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "No se configuró DefaultConnection.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySQL(connectionString));
```

Agregar este fragmento dentro del `Program.cs` generado por ASP.NET Core, después de crear `builder` y antes de ejecutar `builder.Build()`.

Debe existir un único registro de `AddDbContext<AppDbContext>()`.

## 7. Configurar User Secrets

Inicializar:

```powershell
dotnet user-secrets init --project "$api"
```

Agregar la conexión:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Port=3306;Database=control_de_dinero;User=controldinero_app;Password=TU_CONTRASEÑA;" --project "$api"
```

Activar el entorno de desarrollo para las herramientas de EF:

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:DOTNET_ENVIRONMENT = "Development"
```

User Secrets es una solución de configuración local para desarrollo, no un almacén cifrado de secretos de producción.

## 8. Crear la primera migración

Compilar:

```powershell
dotnet build
```

Generar la migración:

```powershell
dotnet ef migrations add InitialCreate --project "$infra" --startup-project "$api" --context AppDbContext
```

Entity Framework Core generará los archivos correspondientes dentro de la carpeta `Migrations`.

El método `Up()` describe los cambios que se aplicarán. El método `Down()` describe cómo revertirlos.

## 9. Revisar las relaciones

Antes de crear tablas, inspeccionar `InitialCreate.cs`.

En nuestro proyecto se decidió utilizar eliminación restringida en las relaciones financieras:

```csharp
onDelete: ReferentialAction.Restrict
```

Esto evita eliminaciones automáticas de cuotas, pagos y registros de auditoría.

También deben verificarse:

- Las claves primarias.
- Las claves foráneas.
- Los índices.
- La precisión de los importes monetarios.
- La obligatoriedad de los campos.

Los importes se almacenan con tipos `decimal`, no `float` ni `double`.

## 10. Inspeccionar el script SQL

Generar un script sin modificar la base de datos:

```powershell
dotnet ef migrations script 0 InitialCreate --project "$infra" --startup-project "$api" --context AppDbContext --output initial-create.sql
```

Revisar el archivo antes de continuar.

## 11. Aplicar la migración

```powershell
dotnet ef database update --project "$infra" --startup-project "$api" --context AppDbContext
```

Este comando aplica las migraciones pendientes sobre la base de datos configurada.

## 12. Verificar desde DBeaver

```sql
USE control_de_dinero;

SHOW TABLES;

SELECT * FROM __EFMigrationsHistory;
```

También puede inspeccionarse la relación entre clientes y préstamos:

```sql
SHOW CREATE TABLE loans;
```

## 13. Crear migraciones posteriores

Cuando cambia el modelo:

```powershell
dotnet ef migrations add NombreDelCambio --project "$infra" --startup-project "$api" --context AppDbContext
```

Después de revisar los cambios:

```powershell
dotnet ef database update --project "$infra" --startup-project "$api" --context AppDbContext
```

Es importante incluir en Git los archivos de migración y el snapshot del modelo.

No eliminar ni modificar una migración que ya fue aplicada a entornos compartidos sin planificar su corrección.

## 14. Errores encontrados durante el desarrollo

### No se encuentra `dotnet ef`

Causa: falta la herramienta CLI.

Solución: instalar `dotnet-ef` en una versión compatible.

### No se reconoce el proyecto

Causa: diferencia entre el nombre de la solución, las carpetas y los proyectos `.csproj`.

Solución: localizar los proyectos mediante PowerShell y utilizar sus rutas reales.

### `mysql_native_password` no está disponible

Causa: MySQL 8.4 deshabilita por defecto ese mecanismo de autenticación.

Solución: configurar el usuario con `caching_sha2_password`.

### Las relaciones aparecen con `Cascade`

Causa: el comportamiento de eliminación configurado o inferido por EF Core.

Solución: definir `DeleteBehavior.Restrict` en el modelo y regenerar una migración que todavía no se haya aplicado.

### La migración genera más tablas de las esperadas

Causa: Entity Framework Core también descubre entidades mediante propiedades de navegación.

Solución: revisar el modelo completo y los archivos generados antes de aplicar la migración.

## 15. Buenas prácticas

- Usar `decimal` para operaciones monetarias.
- Evitar eliminar físicamente información financiera histórica.
- Proteger credenciales mediante configuraciones locales o gestores de secretos.
- Versionar las migraciones junto con el código.
- Revisar scripts SQL antes de aplicarlos a bases de datos importantes.
- Mantener separadas las reglas de negocio y la infraestructura de persistencia.
- Agregar pruebas automatizadas para cálculos, pagos y saldos.

## Resultado

Entity Framework Core queda configurado para conectar las entidades de C# con MySQL, generar migraciones y gestionar la evolución de la estructura de datos.