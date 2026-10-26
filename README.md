# Agenda Salón

[![CI](https://github.com/Faraday08/AgendaSalon/actions/workflows/ci.yml/badge.svg)](https://github.com/Faraday08/AgendaSalon/actions/workflows/ci.yml)

Sistema de reservas para un salón de belleza y barbería. Los clientes ven los servicios, consultan horas libres y reservan su cita. El salón ve su agenda actualizada.

> Estado: **en desarrollo**. Ya funcionan la reserva en línea para clientes y el panel del salón con la agenda en tiempo real. Próximo paso: asistente orientador con IA que recomienda el servicio y reserva por chat.

## Tecnologías

- **Frontend:** React 19, TypeScript, Vite, TanStack Query y cliente de SignalR ([web/](web/README.md))
- ASP.NET Core 10 (Web API) con SignalR
- Autenticación con JWT y roles
- Entity Framework Core 10 con SQL Server y migraciones
- xUnit, con pruebas de dominio, de integración y de punta a punta (`WebApplicationFactory`)

## Arquitectura (Onion)

Las dependencias apuntan siempre hacia el centro. El dominio no conoce la base de datos ni la web.

```
Api  ──►  Infrastructure  ──►  Application  ──►  Domain
(web)     (EF Core, SQL)       (casos de uso)    (reglas del negocio)
```

| Proyecto | Responsabilidad | Depende de |
|---|---|---|
| `AgendaSalon.Domain` | Entidades con sus reglas (`Cita`, `Estilista`, `Servicio`, `Cliente`, `Usuario`), objetos de valor (`Telefono`, `Correo`, `CodigoCita`, `Intervalo`), la calculadora de disponibilidad, la política de contraseñas y las interfaces de los repositorios | Nada |
| `AgendaSalon.Application` | Casos de uso (catálogo, disponibilidad, reservas, login, panel, cuentas), DTOs y abstracciones (`IUnidadDeTrabajo`, `IRelojSalon`, `IHasherContrasenas`, `IGeneradorToken`, `IUsuarioActual`, `INotificadorAgenda`) | Domain |
| `AgendaSalon.Infrastructure` | `DbContext`, configuraciones por entidad, repositorios, unidad de trabajo, migraciones, reloj, hash de contraseñas y emisión de JWT | Application |
| `AgendaSalon.Api` | Controladores, hub de SignalR, autenticación y autorización, límite de intentos de login, traducción de errores a HTTP y composición de dependencias | Application, Infrastructure |

La aplicación no sabe que existe SignalR: avisa a través de `INotificadorAgenda`, y la API lo implementa con el hub. Lo mismo pasa con el usuario actual (`IUsuarioActual`, leído del token).

### Buenas prácticas aplicadas

- **Dominio rico:** las entidades tienen constructores privados, setters privados y métodos de fábrica (`Cita.Reservar`, `Estilista.Contratar`), así que no se puede crear un objeto en estado inválido.
- **Objetos de valor:** `Telefono` normaliza y valida números dominicanos, y `CodigoCita` genera códigos sin caracteres confusos.
- **Reglas en un solo lugar:** las longitudes máximas son constantes del dominio y la configuración de EF Core las reutiliza.
- **Repositorios y unidad de trabajo:** la aplicación no conoce EF Core. La reserva corre en una transacción `Serializable` y los interbloqueos de SQL Server se traducen a 409.
- **Errores con significado:** `ReglaNegocioException` → 400, `NoAutorizadoException` → 401, `ProhibidoException` → 403, `NoEncontradoException` → 404, `ConflictoException` → 409, siempre en formato ProblemDetails.
- **Seguridad:** ver la sección [Panel del salón](#panel-del-salón).
- **Compilación estricta:** `Directory.Build.props` activa nullable y trata las advertencias como errores (incluidas las de paquetes con vulnerabilidades). `.editorconfig` define el estilo de nombres.

## Modelo de datos (normalizado)

```
CategoriasServicio 1──* Servicios *──* Estilistas 1──* HorariosLaborales
                            │   (EstilistaServicios)  │
                            1                         1
                            │                         │
Clientes 1──────────────* Citas *─────────────────────┘
                            *                         │
                            │                         0..1
                       1 EstadosCita (catálogo)   Usuarios *──1 RolesUsuario (catálogo)
```

- Una cuenta de estilista está ligada a su estilista (como máximo una por estilista); un administrador no tiene estilista. La base lo garantiza con un `CHECK` y un índice único filtrado.

- Los datos del cliente viven en `Clientes` (teléfono único) y no se repiten en cada cita.
- Las categorías y los estados son tablas catálogo con claves foráneas, no texto suelto.
- `Citas.PrecioAcordado` y `Citas.Fin` se guardan a propósito: son datos históricos de la reserva. Si mañana cambia el precio o la duración del servicio, las citas ya hechas no cambian.
- Restricciones `CHECK` en la base: duración y precio válidos, jornadas y citas que terminan después de empezar.

## Reglas de negocio

- Las horas se ofrecen cada 15 minutos dentro del horario de cada estilista (admite jornadas partidas).
- No se puede reservar con menos de 60 minutos de anticipación ni con más de 30 días de adelanto.
- Un estilista no puede tener dos citas que se solapen. Si varios clientes piden el mismo hueco a la vez, solo uno lo obtiene.
- Si el cliente no elige estilista, se asigna el primero que esté libre a esa hora.
- Un cliente que vuelve se reconoce por su teléfono.
- Para cancelar se pide el código de la cita y el teléfono con que se reservó.
- Todas las horas son locales del salón (`America/Santo_Domingo`). Se configuran en la sección `Agenda` de `appsettings.json`.

## Panel del salón

El personal entra con correo y contraseña y recibe un **JWT** (válido 60 minutos) que envía en `Authorization: Bearer <token>`.

| Rol | Puede |
|---|---|
| Administrador | Ver y gestionar la agenda de todo el salón y crear cuentas para los estilistas |
| Estilista | Ver y gestionar solo sus propias citas |

**Tiempo real:** el panel se conecta al hub de SignalR `/hubs/agenda` con el mismo token (en el navegador va en `?access_token=`). Recibe `CitaCreada` cuando un cliente reserva y `CitaActualizada` cuando una cita se cancela, se completa o se marca como no asistida. Los administradores reciben todo; cada estilista, solo lo de su agenda.

Medidas de seguridad:

- Contraseñas guardadas con el `PasswordHasher` de ASP.NET Core Identity (PBKDF2 con sal), nunca en claro.
- El login responde igual y tarda lo mismo si el correo no existe o si la contraseña es incorrecta, para no revelar qué cuentas hay.
- Máximo 5 intentos de login por minuto por IP (luego responde 429).
- La clave de firma del JWT y la contraseña del primer administrador **no están en el repositorio**: se leen de user-secrets en desarrollo o de variables de entorno en producción. La API no arranca si la clave mide menos de 32 bytes.
- Los permisos se revisan dos veces: con políticas de autorización en la API y otra vez en los casos de uso.

## Cómo ejecutarlo

Requisitos: .NET 10 y SQL Server. La cadena por defecto usa la instancia local con autenticación de Windows; se cambia en `ConnectionStrings:Agenda`.

La primera vez, configura los secretos locales (se guardan en tu perfil, fuera del repositorio). El script genera una clave aleatoria para el JWT y te pide el correo y la contraseña del primer administrador sin mostrarla:

```bash
powershell -ExecutionPolicy Bypass -File scripts/configurar-secretos.ps1
```

O a mano:

```bash
dotnet user-secrets set "Jwt:Clave" "<al menos 32 caracteres aleatorios>" --project src/AgendaSalon.Api
dotnet user-secrets set "AdminInicial:Correo" "admin@tusalon.com" --project src/AgendaSalon.Api
dotnet user-secrets set "AdminInicial:Contrasena" "<10+ caracteres con letras y números>" --project src/AgendaSalon.Api
```

```bash
dotnet run --project src/AgendaSalon.Api --launch-profile http
```

La API queda en `http://localhost:5063`. Al iniciar aplica las migraciones, crea la base `AgendaSalon` con servicios y estilistas de ejemplo y, si no existe, la cuenta del administrador inicial. El archivo `src/AgendaSalon.Api/AgendaSalon.Api.http` trae peticiones listas para probar.

Con la API corriendo, inicia el frontend (detalles en [web/README.md](web/README.md)):

```bash
cd web
npm install
npm run dev
```

Abre `http://localhost:5173` para reservar como cliente, o `http://localhost:5173/panel` para entrar al panel del salón.

Para crear una migración después de cambiar el modelo:

```bash
dotnet tool restore
dotnet tool run dotnet-ef migrations add NombreDelCambio -p src/AgendaSalon.Infrastructure -s src/AgendaSalon.Api -o Persistencia/Migraciones
```

## Pruebas

```bash
dotnet test
```

- `AgendaSalon.Domain.Tests`: reglas del dominio sin base de datos (disponibilidad, citas, teléfonos, correos, contraseñas, usuarios).
- `AgendaSalon.IntegrationTests`: casos de uso completos con repositorios y EF Core sobre SQLite en memoria, y pruebas de punta a punta que levantan la API real: login, permisos por rol, límite de intentos y avisos por SignalR a cada grupo. No necesitan SQL Server.

GitHub Actions compila la solución y corre todas las pruebas en cada push y en cada pull request a `main` ([.github/workflows/ci.yml](.github/workflows/ci.yml)).

## Endpoints

Públicos (para los clientes):

| Método | Ruta | Descripción |
|---|---|---|
| GET | `/api/servicios` | Servicios con categoría, duración y precio |
| GET | `/api/estilistas?servicioId=` | Estilistas (opcionalmente, solo los que hacen un servicio) |
| GET | `/api/disponibilidad?servicioId=&fecha=&estilistaId=` | Horas libres de un día |
| POST | `/api/citas` | Reservar. Responde 409 si la hora ya no está libre |
| GET | `/api/citas/{codigo}` | Consultar una cita |
| POST | `/api/citas/{codigo}/cancelacion` | Cancelar con el teléfono del cliente |

Panel (requieren token):

| Método | Ruta | Quién | Descripción |
|---|---|---|---|
| POST | `/api/auth/login` | Cualquiera | Inicia sesión y devuelve el JWT |
| GET | `/api/panel/citas?desde=&hasta=&estilistaId=` | Personal | Agenda entre dos fechas (máximo 31 días) |
| POST | `/api/panel/citas/{codigo}/completada` | Personal | Marca la cita como completada |
| POST | `/api/panel/citas/{codigo}/no-asistio` | Personal | Marca que el cliente no vino |
| POST | `/api/panel/citas/{codigo}/cancelacion` | Personal | Cancelación hecha por el salón |
| POST | `/api/panel/usuarios/estilistas` | Administrador | Crea la cuenta de un estilista |
| WS | `/hubs/agenda` | Personal | Avisos en tiempo real (`CitaCreada`, `CitaActualizada`) |
