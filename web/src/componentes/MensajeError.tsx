import { ErrorApi } from '../api/cliente'

/** Muestra el mensaje de un error de la API (ya viene en español) o uno genérico. */
export function MensajeError({ error }: { error: unknown }) {
  if (!error) return null
  const mensaje = error instanceof ErrorApi ? error.message : 'Ocurrió un error inesperado. Inténtalo de nuevo.'
  return (
    <p className="alerta alerta-error" role="alert">
      {mensaje}
    </p>
  )
}
