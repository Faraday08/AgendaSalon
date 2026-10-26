import type { Notificacion } from './useNotificaciones'

/** Mensajes breves abajo a la derecha. Se anuncian a los lectores de pantalla sin interrumpir. */
export function Notificaciones({ lista }: { lista: readonly Notificacion[] }) {
  return (
    <ul className="notificaciones" aria-live="polite" aria-label="Avisos">
      {lista.map((n) => (
        <li key={n.id} className="notificacion">
          {n.texto}
        </li>
      ))}
    </ul>
  )
}
