import type { HuecoDisponible } from '../api/tipos'

/**
 * Con "cualquier estilista" se muestra cada hora una sola vez. La API asigna al primero libre,
 * que es el mismo que queda aquí: ambos ordenan por hora y luego por nombre del estilista.
 */
export function horasUnicas(huecos: readonly HuecoDisponible[]): HuecoDisponible[] {
  const vistas = new Set<string>()
  return huecos.filter((h) => {
    if (vistas.has(h.inicio)) return false
    vistas.add(h.inicio)
    return true
  })
}
