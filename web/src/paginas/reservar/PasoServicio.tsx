import { useQuery } from '@tanstack/react-query'
import { apiReservas } from '../../api/reservas'
import type { Servicio } from '../../api/tipos'
import { MensajeError } from '../../componentes/MensajeError'
import { formatearPrecio } from '../../utilidades/dinero'
import { formatearDuracion } from '../../utilidades/fechas'

interface Props {
  seleccionado: Servicio | null
  alElegir: (servicio: Servicio) => void
}

export function PasoServicio({ seleccionado, alElegir }: Props) {
  const servicios = useQuery({ queryKey: ['servicios'], queryFn: apiReservas.servicios, staleTime: 5 * 60_000 })

  if (servicios.isPending) return <p className="cargando">Cargando servicios…</p>
  if (servicios.isError) return <MensajeError error={servicios.error} />

  const porCategoria = Map.groupBy(servicios.data, (s) => s.categoria)

  return (
    <section aria-labelledby="titulo-servicio">
      <h2 id="titulo-servicio">¿Qué te vas a hacer?</h2>
      {[...porCategoria].map(([categoria, lista]) => (
        <div key={categoria} className="categoria">
          <h3>{categoria}</h3>
          <div className="opciones">
            {lista.map((servicio) => (
              <button
                key={servicio.id}
                type="button"
                className="opcion"
                aria-pressed={seleccionado?.id === servicio.id}
                onClick={() => alElegir(servicio)}
              >
                <span className="opcion-titulo">{servicio.nombre}</span>
                <span className="texto-suave">{servicio.descripcion}</span>
                <span className="opcion-detalle">
                  <span>{formatearDuracion(servicio.duracionMinutos)}</span>
                  <span className="opcion-precio">{formatearPrecio(servicio.precio)}</span>
                </span>
              </button>
            ))}
          </div>
        </div>
      ))}
    </section>
  )
}
