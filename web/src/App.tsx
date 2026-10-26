import { Navigate, Route, Routes, useLocation } from 'react-router-dom'
import type { Servicio } from './api/tipos'
import { DisenoPublico } from './componentes/DisenoPublico'
import { PaginaMiCita } from './paginas/PaginaMiCita'
import { PaginaEntrar } from './paginas/panel/PaginaEntrar'
import { PaginaPanel } from './paginas/panel/PaginaPanel'
import { PaginaReservar } from './paginas/reservar/PaginaReservar'
import { RutaProtegida } from './sesion/RutaProtegida'

export function App() {
  return (
    <Routes>
      <Route element={<DisenoPublico />}>
        <Route index element={<RutaReservar />} />
        <Route path="mi-cita" element={<PaginaMiCita />} />
        <Route path="panel/entrar" element={<PaginaEntrar />} />
      </Route>
      <Route
        path="panel"
        element={
          <RutaProtegida>
            <PaginaPanel />
          </RutaProtegida>
        }
      />
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  )
}

/**
 * El asistente guiado navega aquí con el servicio elegido en el estado de la navegación.
 * La clave de la ubicación reinicia el asistente de reservas en cada navegación, aunque ya estuviera abierto.
 */
function RutaReservar() {
  const ubicacion = useLocation()
  const servicio = (ubicacion.state as { servicio?: Servicio } | null)?.servicio ?? null
  return <PaginaReservar key={ubicacion.key} servicioInicial={servicio} />
}
