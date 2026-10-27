# Guía de instalación

## Objetivo

Esta guía explica cómo descargar y ejecutar **Control de Dinero** en una computadora nueva, utilizando el código fuente y las migraciones existentes.

NO ES NECESARIO instalar los paquetes NuGet de Entity Framework Core uno por uno: están declarados en los archivos `.csproj` del proyecto.

## 1. Requisitos

Instalar:

- Git
- .NET SDK 10
- MySQL Community Server 8.4
- Visual Studio Community 2026, o un entorno compatible
- DBeaver (opcional, mas que nada para consultar la base de datos)

Comprobar que .NET y Git estén disponibles:

```powershell
dotnet --version
git --version
```
De no ser el caso de tener donet, utilice:

dotnet tool install --global dotnet-ef --version 10.0.9


## 2. Clonar el repositorio

Abrir PowerShell y ejecutar:

```powershell
git clone https://github.com/AlonsoT46/loan-management-system
cd CARPETA_DEL_REPOSITORIO
```
Fijese bien que los nombres coincidan

## 3. Restaurar los paquetes NuGet

Desde la raíz de la solución:

```powershell
dotnet restore
dotnet build
```

`dotnet restore` descarga automáticamente las dependencias definidas por los proyectos, incluido Entity Framework Core.

## 4. Preparar MySQL

Abrir DBeaver y conectarse con una cuenta administradora de MySQL.

Ejecutar:

```sql
CREATE DATABASE IF NOT EXISTS control_de_dinero
CHARACTER SET utf8mb4;
```

Crear un usuario para el proyecto:

```sql
CREATE USER 'controldinero_app'@'localhost'
IDENTIFIED WITH caching_sha2_password
BY 'CAMBIAR_POR_CONTRASEÑA_SEGURA';

GRANT SELECT, INSERT, UPDATE, DELETE,
      CREATE, ALTER, DROP, INDEX, REFERENCES
ON control_de_dinero.*
TO 'controldinero_app'@'localhost';
```

Utilizar una contraseña segura y privada. De ser el caso que el usuario ya exista, no vuelva a ejecutar `CREATE USER`.

**Nota:** los permisos `CREATE`, `ALTER` y `DROP` se necesitan para aplicar migraciones. En producción se recomienda utilizar una cuenta distinta, con menores privilegios, para ejecutar la aplicación.

## 5. Configurar la conexión

Desde la raíz de la solución, localizar el proyecto API:

```powershell
$api = (Get-ChildItem -Recurse -Filter "*Api.csproj" |
    Select-Object -First 1).FullName
```

Si todavía no está habilitado User Secrets:

```powershell
dotnet user-secrets init --project "$api"
```

Guardar la cadena de conexión:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Port=3306;Database=control_de_dinero;User=controldinero_app;Password=TU_CONTRASEÑA;" --project "$api"
```

Reemplazar `TU_CONTRASEÑA` por la contraseña configurada en MySQL.

Importante, NO publicar credenciales al repositorio.

## 6. Preparar Entity Framework Core

Los paquetes NuGet ya se restauraron en el paso 3. Únicamente necesitamos comprobar si está disponible la herramienta de migraciones:

```powershell
dotnet ef --version
```

Si no está instalada:

```powershell
dotnet tool install --global dotnet-ef --version 10.0.9
```

Si existe otra versión instalada globalmente y se necesita actualizar:

```powershell
dotnet tool update --global dotnet-ef --version 10.0.9
```

## 7. Crear las tablas con las migraciones existentes

Localizar el proyecto Infrastructure:

```powershell
$infra = (Get-ChildItem -Recurse -Filter "*Infrastructure.csproj" |
    Select-Object -First 1).FullName
```

Activar el entorno de desarrollo:

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:DOTNET_ENVIRONMENT = "Development"
```

Aplicar las migraciones:

```powershell
dotnet ef database update --project "$infra" --startup-project "$api" --context AppDbContext
```

**No ejecutar `migrations add InitialCreate`:** esa migración ya está creada y debe encontrarse en el repositorio (en este caso, es update)

## 8. Verificar las tablas

En DBeaver:

```sql
USE control_de_dinero;

SHOW TABLES;

SELECT * FROM __EFMigrationsHistory;
```

El esquema inicial debe incluir:

- `clients`
- `loans`
- `AuditEvent`
- `Installment`
- `Payment`
- `PaymentAllocation`
- `__EFMigrationsHistory`

Los nombres exactos corresponden a la migración inicial; podrán cambiar mediante futuras migraciones.

## 9. Ejecutar el backend

Desde PowerShell:

```powershell
dotnet run --project "$api"
```

Alternativamente, abrir la solución en Visual Studio, seleccionar el proyecto API como proyecto de inicio y ejecutarlo.

La dirección HTTP o HTTPS aparecerá en la consola. Los endpoints disponibles dependerán de la etapa de desarrollo.

## 10. PROBLEMAS FRECUENTES

Esto sobre los problemas que pueden presentarse durante la instalación

### `dotnet ef` no reconocido

Instalar `dotnet-ef` y reiniciar PowerShell si es necesario.

### `Plugin 'mysql_native_password' is not loaded`

Verificar que la cuenta de MySQL utilice `caching_sha2_password`, compatible con MySQL 8.4.

### Error de conexión a MySQL

Revisar el servicio MySQL, puerto 3306, credenciales, permisos y User Secrets.

En caso de no encontrar User Secrets, ejecute: Wins + R, seguidamente, ejecute: %APPDATA%\Microsoft\UserSecrets presiona enter, abre el archivo y vuelva al paso 5.

### El proyecto API o Infrastructure no se encuentra

Comprobar que se están ejecutando los comandos desde la raíz del repositorio y que los archivos `.csproj` existen.

### Las tablas ya existen

No eliminarlas automáticamente. Revisar el estado de las migraciones y `__EFMigrationsHistory`.
