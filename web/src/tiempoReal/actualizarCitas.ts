import type { CitaPanel } from '../api/tipos'

/**
 * Aplica a la lista de citas de un día una cita que llegó en tiempo real:
 * la reemplaza si ya estaba (mismo código) o la agrega, y deja la lista ordenada por hora.
 * Devuelve una lista nueva sin modificar la original.
 */
export function aplicarCita(citas: readonly CitaPanel[] | undefined, cita: CitaPanel): CitaPanel[] {
  const resto = (citas ?? []).filter((c) => c.codigo !== cita.codigo)
  return [...resto, cita].sort((a, b) => a.inicio.localeCompare(b.inicio) || a.estilista.localeCompare(b.estilista))
}
