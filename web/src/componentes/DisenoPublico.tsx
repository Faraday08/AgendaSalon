import { NavLink, Outlet } from 'react-router-dom'
import { Marca } from './Marca'
import { AsistenteGuiado } from './orientador/AsistenteGuiado'

export function DisenoPublico() {
  return (
    <>
      <header className="encabezado">
        <div className="contenedor">
          <Marca />
          <nav className="navegacion" aria-label="Principal">
            <NavLink to="/" end>
              Reservar
            </NavLink>
            <NavLink to="/mi-cita">Mi cita</NavLink>
            <NavLink to="/panel">Personal</NavLink>
          </nav>
        </div>
      </header>
      <main className="contenedor">
        <Outlet />
      </main>
      <AsistenteGuiado />
    </>
  )
}
