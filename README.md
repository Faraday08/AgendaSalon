# Agenda Salón

Sistema de reservas para un salón de belleza y barbería. Los clientes ven los servicios, consultan horas libres y reservan su cita. El salón ve su agenda actualizada.

> Estado: **backend en desarrollo**. Próximos pasos: panel del salón con calendario en tiempo real (SignalR) y login con JWT, asistente orientador con IA y frontend en React.

## Tecnologías

- ASP.NET Core 10 (Web API)
- Entity Framework Core 10 con SQL Server y migraciones
- xUnit, con pruebas de dominio y de integración

## Arquitectura (Onion)

Las dependencias apuntan siempre hacia el centro. El dominio no conoce la base de datos ni la web.

```
Api  ──►  Infrastructure  ──►  Application  ──►  Domain
(web)     (EF Core, SQL)       (casos de uso)    (reglas del negocio)
```

| Proyecto | Responsabilidad | Depende de |
|---|---|---|
| `AgendaSalon.Domain` | Entidades con sus reglas (`Cita`, `Estilista`, `Servicio`, `Cliente`), objetos de valor (`Telefono`, `CodigoCita`, `Intervalo`), la calculadora de disponibilidad y las interfaces de los repositorios | Nada |
| `AgendaSalon.Application` | Casos de uso (catálogo, disponibilidad, reservas), DTOs y abstracciones (`IUnidadDeTrabajo`, `IRelojSalon`) | Domain |
| `AgendaSalon.Infrastructure` | `DbContext`, configuraciones por entidad, repositorios, unidad de trabajo, migraciones y reloj | Application |
| `AgendaSalon.Api` | Controladores, traducción de errores a HTTP y composición de dependencias | Application, Infrastructure |

### Buenas prácticas aplicadas

- **Dominio rico:** las entidades tienen constructores privados, setters privados y métodos de fábrica (`Cita.Reservar`, `Estilista.Contratar`), así que no se puede crear un objeto en estado inválido.
- **Objetos de valor:** `Telefono` normaliza y valida números dominicanos, y `CodigoCita` genera códigos sin caracteres confusos.
- **Reglas en un solo lugar:** las longitudes máximas son constantes del dominio y la configuración de EF Core las reutiliza.
- **Repositorios y unidad de trabajo:** la aplicación no conoce EF Core. La reserva corre en una transacción `Serializable` y los interbloqueos de SQL Server se traducen a 409.
- **Errores con significado:** `ReglaNegocioException` → 400, `NoEncontradoException` → 404, `ConflictoException` → 409, siempre en formato ProblemDetails.
- **Compilación estricta:** `Directory.Build.props` activa nullable y trata las advertencias como errores (incluidas las de paquetes con vulnerabilidades). `.editorconfig` define el estilo de nombres.

## Modelo de datos (normalizado)

```
CategoriasServicio 1──* Servicios *──* Estilistas 1──* HorariosLaborales
                            │   (EstilistaServicios)  │
                            1                         1
                            │                         │
Clientes 1──────────────* Citas *─────────────────────┘
                            *
                            │
                       1 EstadosCita (catálogo)
```

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

## Cómo ejecutarlo

Requisitos: .NET 10 y SQL Server. La cadena por defecto usa la instancia local con autenticación de Windows; se cambia en `ConnectionStrings:Agenda`.

```bash
dotnet run --project src/AgendaSalon.Api --launch-profile http
```

La API queda en `http://localhost:5063`. Al iniciar aplica las migraciones y crea la base `AgendaSalon` con servicios y estilistas de ejemplo. El archivo `src/AgendaSalon.Api/AgendaSalon.Api.http` trae peticiones listas para probar.

Para crear una migración después de cambiar el modelo:

```bash
dotnet tool restore
dotnet tool run dotnet-ef migrations add NombreDelCambio -p src/AgendaSalon.Infrastructure -s src/AgendaSalon.Api -o Persistencia/Migraciones
```

## Pruebas

```bash
dotnet test
```

- `AgendaSalon.Domain.Tests`: reglas del dominio sin base de datos (disponibilidad, citas, teléfonos, estilistas).
- `AgendaSalon.IntegrationTests`: casos de uso completos con repositorios y EF Core sobre SQLite en memoria, sin necesitar SQL Server.

## Endpoints

| Método | Ruta | Descripción |
|---|---|---|
| GET | `/api/servicios` | Servicios con categoría, duración y precio |
| GET | `/api/estilistas?servicioId=` | Estilistas (opcionalmente, solo los que hacen un servicio) |
| GET | `/api/disponibilidad?servicioId=&fecha=&estilistaId=` | Horas libres de un día |
| POST | `/api/citas` | Reservar. Responde 409 si la hora ya no está libre |
| GET | `/api/citas/{codigo}` | Consultar una cita |
| POST | `/api/citas/{codigo}/cancelacion` | Cancelar con el teléfono del cliente |
