import type { FechaLocal } from '../api/tipos'

/** Clave de TanStack Query para las citas del panel de un día. Sin fecha, abarca todos los días. */
export function claveCitasDelDia(fecha?: FechaLocal) {
  return fecha === undefined ? (['panel', 'citas'] as const) : (['panel', 'citas', fecha] as const)
}
