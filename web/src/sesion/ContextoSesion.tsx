import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from 'react'
import { alRechazarToken } from '../api/cliente'
import { apiPanel } from '../api/panel'
import type { Sesion } from '../api/tipos'
import { borrarSesion, guardarSesion, leerSesion } from './almacenSesion'

interface ValorSesion {
  sesion: Sesion | null
  iniciarSesion: (correo: string, contrasena: string) => Promise<void>
  cerrarSesion: () => void
}

const ContextoSesion = createContext<ValorSesion | null>(null)

// setTimeout desborda por encima de ~24,8 días y se dispararía de inmediato.
const EsperaMaximaMs = 2_147_483_647

export function ProveedorSesion({ children }: { children: ReactNode }) {
  const [sesion, setSesion] = useState<Sesion | null>(() => leerSesion())

  const cerrarSesion = useCallback(() => {
    borrarSesion()
    setSesion(null)
  }, [])

  // Si la API rechaza el token (por ejemplo, porque expiró), se cierra la sesión.
  useEffect(() => alRechazarToken(cerrarSesion), [cerrarSesion])

  // Cierra la sesión justo cuando el token expira.
  useEffect(() => {
    if (!sesion) return
    const restante = Date.parse(sesion.expiraEnUtc) - Date.now()
    if (restante > EsperaMaximaMs) return
    const temporizador = window.setTimeout(cerrarSesion, Math.max(restante, 0))
    return () => window.clearTimeout(temporizador)
  }, [sesion, cerrarSesion])

  const iniciarSesion = useCallback(async (correo: string, contrasena: string) => {
    const nueva = await apiPanel.iniciarSesion(correo, contrasena)
    guardarSesion(nueva)
    setSesion(nueva)
  }, [])

  const valor = useMemo(() => ({ sesion, iniciarSesion, cerrarSesion }), [sesion, iniciarSesion, cerrarSesion])
  return <ContextoSesion.Provider value={valor}>{children}</ContextoSesion.Provider>
}

// oxlint-disable-next-line react/only-export-components -- el hook va junto a su proveedor
export function useSesion(): ValorSesion {
  const valor = useContext(ContextoSesion)
  if (!valor) throw new Error('useSesion debe usarse dentro de <ProveedorSesion>.')
  return valor
}
