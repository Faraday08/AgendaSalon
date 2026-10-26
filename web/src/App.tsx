import { Navigate, Route, Routes } from 'react-router-dom'
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
        <Route index element={<PaginaReservar />} />
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
