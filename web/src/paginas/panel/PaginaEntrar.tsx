import { useId, useState, type FormEvent } from 'react'
import { Navigate, useLocation, useNavigate } from 'react-router-dom'
import { Marca } from '../../componentes/Marca'
import { MensajeError } from '../../componentes/MensajeError'
import { useSesion } from '../../sesion/ContextoSesion'

export function PaginaEntrar() {
  const id = useId()
  const { sesion, iniciarSesion } = useSesion()
  const navegar = useNavigate()
  const ubicacion = useLocation()
  const destino = (ubicacion.state as { desde?: string } | null)?.desde ?? '/panel'

  const [correo, setCorreo] = useState('')
  const [contrasena, setContrasena] = useState('')
  const [enviando, setEnviando] = useState(false)
  const [error, setError] = useState<unknown>(null)

  if (sesion) return <Navigate to={destino} replace />

  const enviar = async (e: FormEvent) => {
    e.preventDefault()
    setEnviando(true)
    setError(null)
    try {
      await iniciarSesion(correo, contrasena)
      navegar(destino, { replace: true })
    } catch (err) {
      setError(err)
      setContrasena('')
    } finally {
      setEnviando(false)
    }
  }

  return (
    <div className="pantalla-centrada">
      <form className="tarjeta pila" onSubmit={enviar} aria-labelledby={`${id}-titulo`}>
        <Marca />
        <div>
          <h1 id={`${id}-titulo`}>Panel del salón</h1>
          <p className="texto-suave">Entra con la cuenta que te dio el administrador.</p>
        </div>

        <div className="campo">
          <label htmlFor={`${id}-correo`}>Correo</label>
          <input
            id={`${id}-correo`}
            type="email"
            autoComplete="username"
            value={correo}
            onChange={(e) => setCorreo(e.target.value)}
            required
          />
        </div>

        <div className="campo">
          <label htmlFor={`${id}-contrasena`}>Contraseña</label>
          <input
            id={`${id}-contrasena`}
            type="password"
            autoComplete="current-password"
            value={contrasena}
            onChange={(e) => setContrasena(e.target.value)}
            required
          />
        </div>

        <MensajeError error={error} />

        <button type="submit" className="boton boton-primario" disabled={enviando}>
          {enviando ? 'Entrando…' : 'Entrar'}
        </button>
      </form>
    </div>
  )
}
