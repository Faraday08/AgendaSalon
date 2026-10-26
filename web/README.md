# Agenda Salón · Frontend

Aplicación web en React para la [API de Agenda Salón](../README.md). Tiene dos partes:

- **Reservas (pública):** el cliente elige servicio, estilista (o "cualquiera"), día y hora; deja su nombre y teléfono y recibe un código. En **Mi cita** consulta o cancela con ese código.
- **Asistente con IA:** un chat flotante en las páginas públicas que recomienda el servicio, busca horas y reserva cuando el cliente confirma. Solo aparece si la API tiene configurada la clave de Claude.
- **Panel del salón (privado):** el personal entra con su cuenta y ve la agenda del día con una columna por estilista. Las citas nuevas y los cambios llegan **en tiempo real** por SignalR, sin recargar. Desde la ficha de una cita se marca como completada, no asistida o cancelada.

## Tecnologías

React 19 · TypeScript (modo estricto) · Vite · React Router · TanStack Query · `@microsoft/signalr` · Vitest + Testing Library · oxlint

## Cómo ejecutarlo

Con la API corriendo en `http://localhost:5063`:

```bash
npm install
npm run dev
```

Abre `http://localhost:5173`. Vite reenvía `/api` y `/hubs` a la API, así que en desarrollo no hace falta configurar CORS. Para otra dirección de la API: `API_URL=http://otra:puerto npm run dev`.

En producción, la variable `VITE_API_URL` indica dónde está la API.

## Scripts

| Script | Qué hace |
|---|---|
| `npm run dev` | Servidor de desarrollo |
| `npm run build` | Revisa los tipos y genera `dist/` |
| `npm run typecheck` | Solo revisa los tipos |
| `npm run lint` | oxlint, con las advertencias tratadas como errores |
| `npm test` | Pruebas en modo observación (`npm run test:ci` para una sola pasada) |

## Organización

```
src/
  api/          Cliente HTTP, tipos de la API y llamadas de reservas y panel
  sesion/       Sesión del personal (token JWT) y ruta protegida
  tiempoReal/   Conexión a SignalR y actualización de la caché de citas
  paginas/      Reservar, Mi cita, login y panel con el calendario
  componentes/  Piezas compartidas (marca, diseño, mensajes de error)
  utilidades/   Fechas, precios y horarios
```

Decisiones:

- **Horas sin zona horaria.** La API usa la hora local del salón (`2026-10-29T10:00:00`). El frontend lee los componentes del texto en vez de convertir zonas, así una cita de las 10:00 se ve a las 10:00 en cualquier dispositivo.
- **Tiempo real sobre la caché.** Cada aviso de SignalR se aplica directamente a la consulta del día en TanStack Query; al reconectar, se recargan los datos por si se perdió algún aviso.
- **Errores en español.** La API ya devuelve mensajes listos para mostrar (ProblemDetails). Si alguien toma la hora mientras el cliente llena sus datos (409), se le devuelve a elegir horario con datos frescos.
- **Accesibilidad.** Etiquetas en todos los campos, foco visible, `<dialog>` nativo, avisos con `aria-live` y respeto a `prefers-reduced-motion`. Modo oscuro automático.
