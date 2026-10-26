import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useId, useState, type FormEvent } from 'react'
import { useSearchParams } from 'react-router-dom'
import { apiReservas } from '../api/reservas'
import { NombreEstado, type Cita } from '../api/tipos'
import { MensajeError } from '../componentes/MensajeError'
import { formatearPrecio } from '../utilidades/dinero'
import { formatearDia, formatearHora } from '../utilidades/fechas'

/** Consulta de una cita por su código y cancelación con el teléfono con que se reservó. */
export function PaginaMiCita() {
  const id = useId()
  const [parametros, setParametros] = useSearchParams()
  const codigo = parametros.get('codigo')?.trim().toUpperCase() ?? ''
  const [codigoEscrito, setCodigoEscrito] = useState(codigo)

  const cita = useQuery({
    queryKey: ['cita', codigo],
    queryFn: () => apiReservas.obtenerCita(codigo),
    enabled: codigo.length > 0,
    retry: false,
  })

  const buscar = (e: FormEvent) => {
    e.preventDefault()
    setParametros(codigoEscrito.trim() ? { codigo: codigoEscrito.trim().toUpperCase() } : {})
  }

  return (
    <>
      <div className="portada">
        <h1>Mi cita</h1>
        <p>Escribe el código que recibiste al reservar para ver los detalles o cancelar.</p>
      </div>

      <div className="pila">
        <form className="tarjeta fila" onSubmit={buscar}>
          <div className="campo campo-flexible">
            <label htmlFor={`${id}-codigo`}>Código de la cita</label>
            <input
              id={`${id}-codigo`}
              value={codigoEscrito}
              onChange={(e) => setCodigoEscrito(e.target.value)}
              placeholder="Por ejemplo: K7PQ2M"
              maxLength={6}
              autoCapitalize="characters"
              autoComplete="off"
              required
            />
          </div>
          <button type="submit" className="boton boton-primario alinear-abajo">
            Buscar
          </button>
        </form>

        {cita.isFetching && <p className="cargando">Buscando tu cita…</p>}
        {cita.isError && <MensajeError error={cita.error} />}
        {cita.data && <DetalleCitaCliente cita={cita.data} />}
      </div>
    </>
  )
}

function DetalleCitaCliente({ cita }: { cita: Cita }) {
  const id = useId()
  const clienteConsultas = useQueryClient()
  const [telefono, setTelefono] = useState('')

  const cancelar = useMutation({
    mutationFn: () => apiReservas.cancelarCita(cita.codigo, telefono),
    onSuccess: (actualizada) => {
      clienteConsultas.setQueryData(['cita', cita.codigo], actualizada)
      void clienteConsultas.invalidateQueries({ queryKey: ['disponibilidad'] })
    },
  })

  const enviar = (e: FormEvent) => {
    e.preventDefault()
    cancelar.mutate()
  }

  return (
    <section className="tarjeta pila" aria-labelledby={`${id}-titulo`}>
      <div className="fila fila-espaciada">
        <h2 id={`${id}-titulo`}>Cita {cita.codigo}</h2>
        <span className={`insignia estado-${cita.estado}`}>{NombreEstado[cita.estado]}</span>
      </div>

      <dl className="resumen">
        <dt>Nombre</dt>
        <dd>{cita.clienteNombre}</dd>
        <dt>Servicio</dt>
        <dd>{cita.servicio}</dd>
        <dt>Día</dt>
        <dd>{formatearDia(cita.inicio)}</dd>
        <dt>Hora</dt>
        <dd>
          {formatearHora(cita.inicio)} a {formatearHora(cita.fin)}
        </dd>
        <dt>Estilista</dt>
        <dd>{cita.estilista}</dd>
        <dt>Precio</dt>
        <dd>{formatearPrecio(cita.precio)}</dd>
      </dl>

      {cancelar.isSuccess && (
        <p className="alerta alerta-exito" role="status">
          Tu cita quedó cancelada. ¡Esperamos verte pronto!
        </p>
      )}

      {cita.estado === 'Confirmada' && (
        <form onSubmit={enviar} className="pila">
          <h3>¿No puedes venir?</h3>
          <div className="campo">
            <label htmlFor={`${id}-telefono`}>Teléfono con el que reservaste</label>
            <input
              id={`${id}-telefono`}
              type="tel"
              inputMode="tel"
              autoComplete="tel"
              value={telefono}
              onChange={(e) => setTelefono(e.target.value)}
              required
            />
          </div>
          <MensajeError error={cancelar.error} />
          <div>
            <button type="submit" className="boton boton-peligro" disabled={cancelar.isPending}>
              {cancelar.isPending ? 'Cancelando…' : 'Cancelar mi cita'}
            </button>
          </div>
        </form>
      )}
    </section>
  )
}
