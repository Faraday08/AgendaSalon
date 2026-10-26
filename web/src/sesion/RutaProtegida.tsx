import type { ReactNode } from 'react'
import { Navigate, useLocation } from 'react-router-dom'
import { useSesion } from './ContextoSesion'

/** Envía al login a quien no tiene sesión, recordando a dónde quería ir. */
export function RutaProtegida({ children }: { children: ReactNode }) {
  const { sesion } = useSesion()
  const ubicacion = useLocation()

  if (!sesion) return <Navigate to="/panel/entrar" replace state={{ desde: ubicacion.pathname + ubicacion.search }} />
  return children
}
