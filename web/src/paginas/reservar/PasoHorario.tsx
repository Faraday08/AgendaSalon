import { useQuery } from '@tanstack/react-query'
import { useId } from 'react'
import { apiReservas } from '../../api/reservas'
import type { FechaHoraLocal, FechaLocal, HuecoDisponible, Servicio } from '../../api/tipos'
import { MensajeError } from '../../componentes/MensajeError'
import { formatearDiaCorto, formatearHora, hoy, sumarDias } from '../../utilidades/fechas'
import { horasUnicas } from '../../utilidades/horarios'

const DiasMaximosAdelanto = 30
const DiasRapidos = 7

interface Props {
  servicio: Servicio
  estilistaId: number | null
  fecha: FechaLocal
  inicio: FechaHoraLocal | null
  alCambiarEstilista: (estilistaId: number | null) => void
  alCambiarFecha: (fecha: FechaLocal) => void
  alElegirHora: (hueco: HuecoDisponible) => void
}

export function PasoHorario(props: Props) {
  const { servicio, estilistaId, fecha, inicio } = props
  const idEstilista = useId()
  const idFecha = useId()
  const primerDia = hoy()

  const estilistas = useQuery({
    queryKey: ['estilistas', servicio.id],
    queryFn: () => apiReservas.estilistas(servicio.id),
    staleTime: 5 * 60_000,
  })
  const huecos = useQuery({
    queryKey: ['disponibilidad', servicio.id, fecha, estilistaId],
    queryFn: () => apiReservas.disponibilidad(servicio.id, fecha, estilistaId),
  })

  const horas = huecos.data ? horasUnicas(huecos.data) : []

  return (
    <section aria-labelledby="titulo-horario" className="pila">
      <h2 id="titulo-horario">¿Cuándo te viene bien?</h2>

      <div className="campo">
        <label htmlFor={idEstilista}>Estilista</label>
        <select
          id={idEstilista}
          value={estilistaId ?? ''}
          onChange={(e) => props.alCambiarEstilista(e.target.value === '' ? null : Number(e.target.value))}
        >
          <option value="">Cualquiera disponible</option>
          {estilistas.data?.map((e) => (
            <option key={e.id} value={e.id}>
              {e.nombre} · {e.especialidad}
            </option>
          ))}
        </select>
      </div>

      <div className="campo">
        <label htmlFor={idFecha}>Día</label>
        <div className="fila" role="group" aria-label="Próximos días">
          {Array.from({ length: DiasRapidos }, (_, i) => sumarDias(primerDia, i)).map((dia) => (
            <button
              key={dia}
              type="button"
              className="boton boton-secundario boton-pequeno chip-dia"
              aria-pressed={dia === fecha}
              onClick={() => props.alCambiarFecha(dia)}
            >
              {dia === primerDia ? 'Hoy' : formatearDiaCorto(dia)}
            </button>
          ))}
          <input
            id={idFecha}
            type="date"
            value={fecha}
            min={primerDia}
            max={sumarDias(primerDia, DiasMaximosAdelanto)}
            onChange={(e) => e.target.value && props.alCambiarFecha(e.target.value)}
            className="selector-fecha"
          />
        </div>
      </div>

      <div className="campo">
        <span id="titulo-horas" className="etiqueta">
          Horas libres
        </span>
        {huecos.isPending && <p className="cargando">Buscando horas libres…</p>}
        {huecos.isError && <MensajeError error={huecos.error} />}
        {huecos.isSuccess && horas.length === 0 && (
          <p className="vacio">No quedan horas libres este día. Prueba con otro día u otro estilista.</p>
        )}
        {horas.length > 0 && (
          <div className="horas" role="group" aria-labelledby="titulo-horas">
            {horas.map((hueco) => (
              <button
                key={hueco.inicio}
                type="button"
                className="hora"
                aria-pressed={hueco.inicio === inicio}
                onClick={() => props.alElegirHora(hueco)}
              >
                {formatearHora(hueco.inicio)}
              </button>
            ))}
          </div>
        )}
      </div>
    </section>
  )
}
