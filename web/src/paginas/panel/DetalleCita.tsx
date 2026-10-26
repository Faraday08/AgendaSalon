import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useEffect, useRef } from 'react'
import { apiPanel, type AccionCita } from '../../api/panel'
import { NombreEstado, type CitaPanel } from '../../api/tipos'
import { MensajeError } from '../../componentes/MensajeError'
import { aplicarCita } from '../../tiempoReal/actualizarCitas'
import { claveCitasDelDia } from '../../tiempoReal/clavesConsulta'
import { formatearPrecio } from '../../utilidades/dinero'
import { fechaDe, formatearDia, formatearHora, leerFecha } from '../../utilidades/fechas'

interface Props {
  cita: CitaPanel | null
  /** Hora actual, para decidir qué acciones tienen sentido (no se completa una cita que no ha empezado). */
  ahora: Date
  alCerrar: () => void
}

/** Ficha de una cita con las acciones que permite su estado. Usa el <dialog> nativo del navegador. */
export function DetalleCita({ cita, ahora, alCerrar }: Props) {
  const dialogo = useRef<HTMLDialogElement>(null)
  const clienteConsultas = useQueryClient()

  const cambiar = useMutation({
    mutationFn: ({ codigo, accion }: { codigo: string; accion: AccionCita }) => apiPanel.cambiarEstado(codigo, accion),
    onSuccess: (actualizada) => {
      clienteConsultas.setQueryData<CitaPanel[]>(claveCitasDelDia(fechaDe(actualizada.inicio)), (actuales) =>
        aplicarCita(actuales, actualizada),
      )
      alCerrar()
    },
  })
  const { reset } = cambiar

  useEffect(() => {
    const elemento = dialogo.current
    if (!elemento) return
    if (cita && !elemento.open) {
      reset()
      elemento.showModal()
    } else if (!cita && elemento.open) {
      elemento.close()
    }
  }, [cita, reset])

  const yaEmpezo = cita ? leerFecha(cita.inicio) <= ahora : false
  const yaTermino = cita ? leerFecha(cita.fin) <= ahora : false
  const confirmada = cita?.estado === 'Confirmada'
  const accion = (a: AccionCita) => cita && cambiar.mutate({ codigo: cita.codigo, accion: a })

  return (
    <dialog ref={dialogo} className="dialogo" onClose={alCerrar} aria-labelledby="titulo-detalle">
      {cita && (
        <div className="pila">
          <div className="fila fila-espaciada">
            <h2 id="titulo-detalle">{cita.clienteNombre}</h2>
            <span className={`insignia estado-${cita.estado}`}>{NombreEstado[cita.estado]}</span>
          </div>

          <dl className="resumen">
            <dt>Servicio</dt>
            <dd>{cita.servicio}</dd>
            <dt>Cuándo</dt>
            <dd>
              {formatearDia(cita.inicio)}, {formatearHora(cita.inicio)} a {formatearHora(cita.fin)}
            </dd>
            <dt>Estilista</dt>
            <dd>{cita.estilista}</dd>
            <dt>Teléfono</dt>
            <dd>
              <a href={`tel:+1${cita.clienteTelefono.replaceAll('-', '')}`}>{cita.clienteTelefono}</a>
            </dd>
            <dt>Precio</dt>
            <dd>{formatearPrecio(cita.precio)}</dd>
            <dt>Código</dt>
            <dd>{cita.codigo}</dd>
            {cita.notas && (
              <>
                <dt>Notas</dt>
                <dd>{cita.notas}</dd>
              </>
            )}
          </dl>

          <MensajeError error={cambiar.error} />

          <div className="fila">
            {confirmada && yaEmpezo && (
              <>
                <button type="button" className="boton boton-exito" onClick={() => accion('completada')} disabled={cambiar.isPending}>
                  Completada
                </button>
                <button type="button" className="boton boton-secundario" onClick={() => accion('no-asistio')} disabled={cambiar.isPending}>
                  No asistió
                </button>
              </>
            )}
            {confirmada && !yaTermino && (
              <button type="button" className="boton boton-peligro" onClick={() => accion('cancelacion')} disabled={cambiar.isPending}>
                Cancelar cita
              </button>
            )}
            <button type="button" className="boton boton-secundario" onClick={alCerrar}>
              Cerrar
            </button>
          </div>
        </div>
      )}
    </dialog>
  )
}
