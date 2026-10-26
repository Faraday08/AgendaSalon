import type { Sesion } from '../api/tipos'

// La sesión se guarda en localStorage para sobrevivir a recargas. Al leerla se descarta si ya expiró.
// Nota: un token en localStorage es legible por cualquier script de la página; por eso el token dura poco
// y la API valida los permisos en cada petición.

const Clave = 'agenda-salon.sesion'

export function leerSesion(ahora = Date.now()): Sesion | null {
  try {
    const texto = localStorage.getItem(Clave)
    if (!texto) return null
    const sesion = JSON.parse(texto) as Sesion
    if (!sesion.token || Date.parse(sesion.expiraEnUtc) <= ahora) {
      localStorage.removeItem(Clave)
      return null
    }
    return sesion
  } catch {
    return null
  }
}

export function guardarSesion(sesion: Sesion) {
  try {
    localStorage.setItem(Clave, JSON.stringify(sesion))
  } catch {
    // Navegación privada o almacenamiento lleno: la sesión dura solo mientras la pestaña esté abierta.
  }
}

export function borrarSesion() {
  try {
    localStorage.removeItem(Clave)
  } catch {
    // Nada que borrar.
  }
}
