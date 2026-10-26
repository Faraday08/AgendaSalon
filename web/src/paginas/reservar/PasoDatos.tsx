import { useId, useState, type FormEvent } from 'react'
import type { HuecoDisponible, Servicio } from '../../api/tipos'
import { MensajeError } from '../../componentes/MensajeError'
import { formatearPrecio } from '../../utilidades/dinero'
import { formatearDia, formatearHora } from '../../utilidades/fechas'

export interface DatosCliente {
  nombre: string
  telefono: string
  notas: string
}

interface Props {
  servicio: Servicio
  hueco: HuecoDisponible
  estilistaElegido: boolean
  enviando: boolean
  error: unknown
  alConfirmar: (datos: DatosCliente) => void
  alVolver: () => void
}

export function PasoDatos({ servicio, hueco, estilistaElegido, enviando, error, alConfirmar, alVolver }: Props) {
  const id = useId()
  const [datos, setDatos] = useState<DatosCliente>({ nombre: '', telefono: '', notas: '' })

  const cambiar = (campo: keyof DatosCliente) => (e: { target: { value: string } }) =>
    setDatos((d) => ({ ...d, [campo]: e.target.value }))

  const enviar = (e: FormEvent) => {
    e.preventDefault()
    alConfirmar(datos)
  }

  return (
    <form onSubmit={enviar} className="pila" aria-labelledby={`${id}-titulo`}>
      <h2 id={`${id}-titulo`}>Tus datos</h2>

      <dl className="resumen tarjeta">
        <dt>Servicio</dt>
        <dd>{servicio.nombre}</dd>
        <dt>Día</dt>
        <dd>{formatearDia(hueco.inicio)}</dd>
        <dt>Hora</dt>
        <dd>
          {formatearHora(hueco.inicio)} a {formatearHora(hueco.fin)}
        </dd>
        <dt>{estilistaElegido ? 'Estilista' : 'Te atiende'}</dt>
        <dd>{hueco.estilistaNombre}</dd>
        <dt>Precio</dt>
        <dd>{formatearPrecio(servicio.precio)}</dd>
      </dl>

      <div className="campo">
        <label htmlFor={`${id}-nombre`}>Nombre</label>
        <input
          id={`${id}-nombre`}
          value={datos.nombre}
          onChange={cambiar('nombre')}
          autoComplete="name"
          maxLength={80}
          required
        />
      </div>

      <div className="campo">
        <label htmlFor={`${id}-telefono`}>Teléfono</label>
        <input
          id={`${id}-telefono`}
          type="tel"
          value={datos.telefono}
          onChange={cambiar('telefono')}
          autoComplete="tel"
          inputMode="tel"
          placeholder="809-555-1234"
          aria-describedby={`${id}-ayuda-telefono`}
          required
        />
        <span id={`${id}-ayuda-telefono`} className="ayuda">
          Lo usaremos para confirmar tu cita. También te lo pediremos si quieres cancelarla.
        </span>
      </div>

      <div className="campo">
        <label htmlFor={`${id}-notas`}>Notas (opcional)</label>
        <textarea
          id={`${id}-notas`}
          value={datos.notas}
          onChange={cambiar('notas')}
          maxLength={300}
          placeholder="Por ejemplo: es mi primera vez, tengo el cabello largo…"
        />
      </div>

      <MensajeError error={error} />

      <div className="acciones-paso">
        <button type="button" className="boton boton-secundario" onClick={alVolver} disabled={enviando}>
          Cambiar hora
        </button>
        <button type="submit" className="boton boton-primario" disabled={enviando}>
          {enviando ? 'Reservando…' : 'Confirmar reserva'}
        </button>
      </div>
    </form>
  )
}
