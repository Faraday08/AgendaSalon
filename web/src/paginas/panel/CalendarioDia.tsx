import type { CSSProperties } from 'react'
import { NombreEstado, type CitaPanel, type Estilista, type FechaLocal } from '../../api/tipos'
import { formatearHora, minutosDelDia } from '../../utilidades/fechas'

const MinutosPorFranja = 15
const HoraInicioPorDefecto = 8
const HoraFinPorDefecto = 21

interface Props {
  fecha: FechaLocal
  estilistas: readonly Estilista[]
  citas: readonly CitaPanel[]
  recientes: ReadonlySet<string>
  /** Minutos desde la medianoche para dibujar la línea de "ahora"; null si el día no es hoy. */
  ahoraMinutos: number | null
  alElegirCita: (cita: CitaPanel) => void
}

/** Rango de horas visible: el horario típico del salón, ampliado si alguna cita queda fuera. */
function rangoVisible(citas: readonly CitaPanel[]) {
  let inicio = HoraInicioPorDefecto * 60
  let fin = HoraFinPorDefecto * 60
  for (const cita of citas) {
    inicio = Math.min(inicio, Math.floor(minutosDelDia(cita.inicio) / 60) * 60)
    fin = Math.max(fin, Math.ceil(minutosDelDia(cita.fin) / 60) * 60)
  }
  return { inicio, fin }
}

/** Vista de un día con una columna por estilista y las citas ubicadas según su hora. */
export function CalendarioDia({ fecha, estilistas, citas, recientes, ahoraMinutos, alElegirCita }: Props) {
  const rango = rangoVisible(citas)
  const franjas = (rango.fin - rango.inicio) / MinutosPorFranja
  const horas = Array.from({ length: (rango.fin - rango.inicio) / 60 + 1 }, (_, i) => rango.inicio / 60 + i)
  const posicion = (minutos: number) => `calc(${(minutos - rango.inicio) / MinutosPorFranja} * var(--alto-franja))`

  const estilo = { '--columnas': Math.max(estilistas.length, 1), '--franjas': franjas } as CSSProperties

  if (estilistas.length === 0) {
    return <p className="vacio">No hay estilistas para mostrar.</p>
  }

  return (
    <div className="calendario" style={estilo} role="region" aria-label={`Agenda del ${fecha}`}>
      <div className="calendario-esquina" />
      {estilistas.map((e) => (
        <div key={e.id} className="calendario-cabecera">
          {e.nombre}
          <small>{e.especialidad}</small>
        </div>
      ))}

      <div className="calendario-horas" aria-hidden="true">
        {horas.map((hora) => (
          <span key={hora} style={{ top: posicion(hora * 60) }}>
            {hora}:00
          </span>
        ))}
      </div>

      {estilistas.map((estilista) => (
        <div key={estilista.id} className="calendario-columna">
          {ahoraMinutos !== null && ahoraMinutos >= rango.inicio && ahoraMinutos <= rango.fin && (
            <div className="calendario-ahora" style={{ top: posicion(ahoraMinutos) }} aria-hidden="true" />
          )}
          {citas
            .filter((c) => c.estilistaId === estilista.id)
            .map((cita) => {
              const inicio = minutosDelDia(cita.inicio)
              const fin = minutosDelDia(cita.fin)
              return (
                <button
                  key={cita.codigo}
                  type="button"
                  className={`bloque-cita estado-${cita.estado}${recientes.has(cita.codigo) ? ' reciente' : ''}`}
                  style={{ top: posicion(inicio), height: `calc(${(fin - inicio) / MinutosPorFranja} * var(--alto-franja) - 2px)` }}
                  onClick={() => alElegirCita(cita)}
                  aria-label={`${formatearHora(cita.inicio)}, ${cita.servicio}, ${cita.clienteNombre}, ${NombreEstado[cita.estado]}`}
                >
                  <strong>
                    {formatearHora(cita.inicio)} · {cita.clienteNombre}
                  </strong>
                  <span>{cita.servicio}</span>
                </button>
              )
            })}
        </div>
      ))}
    </div>
  )
}
