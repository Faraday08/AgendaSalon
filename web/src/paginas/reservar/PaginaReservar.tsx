import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { ErrorApi } from '../../api/cliente'
import { apiReservas } from '../../api/reservas'
import type { Cita, FechaLocal, HuecoDisponible, Servicio } from '../../api/tipos'
import { hoy } from '../../utilidades/fechas'
import { PasoDatos, type DatosCliente } from './PasoDatos'
import { PasoHorario } from './PasoHorario'
import { PasoServicio } from './PasoServicio'
import { ReservaConfirmada } from './ReservaConfirmada'

type Paso = 'servicio' | 'horario' | 'datos'
const Pasos: { id: Paso; nombre: string }[] = [
  { id: 'servicio', nombre: 'Servicio' },
  { id: 'horario', nombre: 'Día y hora' },
  { id: 'datos', nombre: 'Tus datos' },
]

/** Con <paramref name="servicioInicial"/> (elegido en el asistente guiado) empieza directamente en el día y la hora. */
export function PaginaReservar({ servicioInicial = null }: { servicioInicial?: Servicio | null }) {
  const clienteConsultas = useQueryClient()
  const [paso, setPaso] = useState<Paso>(servicioInicial ? 'horario' : 'servicio')
  const [servicio, setServicio] = useState<Servicio | null>(servicioInicial)
  const [estilistaId, setEstilistaId] = useState<number | null>(null)
  const [fecha, setFecha] = useState<FechaLocal>(hoy)
  const [hueco, setHueco] = useState<HuecoDisponible | null>(null)
  const [citaCreada, setCitaCreada] = useState<Cita | null>(null)

  const reservar = useMutation({
    mutationFn: (datos: DatosCliente) =>
      apiReservas.crearCita({
        servicioId: servicio!.id,
        estilistaId,
        inicio: hueco!.inicio,
        clienteNombre: datos.nombre,
        clienteTelefono: datos.telefono,
        notas: datos.notas.trim() || null,
      }),
    onSuccess: (cita) => {
      setCitaCreada(cita)
      void clienteConsultas.invalidateQueries({ queryKey: ['disponibilidad'] })
    },
    onError: (error) => {
      // Alguien tomó la hora mientras el cliente escribía: se vuelve a elegir con datos frescos.
      if (error instanceof ErrorApi && error.estado === 409) {
        setHueco(null)
        setPaso('horario')
        void clienteConsultas.invalidateQueries({ queryKey: ['disponibilidad'] })
      }
    },
  })

  const empezarDeNuevo = () => {
    setPaso('servicio')
    setServicio(null)
    setEstilistaId(null)
    setFecha(hoy())
    setHueco(null)
    setCitaCreada(null)
    reservar.reset()
  }

  if (citaCreada) {
    return (
      <div className="tarjeta">
        <ReservaConfirmada cita={citaCreada} alReservarOtra={empezarDeNuevo} />
      </div>
    )
  }

  const indicePaso = Pasos.findIndex((p) => p.id === paso)
  const conflicto = reservar.error instanceof ErrorApi && reservar.error.estado === 409 ? reservar.error : null

  return (
    <>
      <div className="portada">
        <h1>Reserva tu cita</h1>
        <p>Elige el servicio, el día y la hora. Te toma menos de un minuto y recibes un código para gestionarla.</p>
      </div>

      <ol className="pasos" aria-label="Pasos de la reserva">
        {Pasos.map((p, i) => (
          <li key={p.id} aria-current={p.id === paso ? 'step' : undefined} className={i < indicePaso ? 'completado' : undefined}>
            {p.nombre}
          </li>
        ))}
      </ol>

      <div className="tarjeta">
        {paso === 'servicio' && (
          <PasoServicio
            seleccionado={servicio}
            alElegir={(s) => {
              setServicio(s)
              setEstilistaId(null)
              setHueco(null)
              setPaso('horario')
            }}
          />
        )}

        {paso === 'horario' && servicio && (
          <>
            {conflicto && (
              <p className="alerta alerta-aviso" role="alert">
                {conflicto.message}
              </p>
            )}
            <PasoHorario
              servicio={servicio}
              estilistaId={estilistaId}
              fecha={fecha}
              inicio={hueco?.inicio ?? null}
              alCambiarEstilista={(id) => {
                setEstilistaId(id)
                setHueco(null)
              }}
              alCambiarFecha={(f) => {
                setFecha(f)
                setHueco(null)
              }}
              alElegirHora={(h) => {
                setHueco(h)
                reservar.reset()
                setPaso('datos')
              }}
            />
            <div className="acciones-paso">
              <button type="button" className="boton boton-secundario" onClick={() => setPaso('servicio')}>
                Cambiar servicio
              </button>
            </div>
          </>
        )}

        {paso === 'datos' && servicio && hueco && (
          <PasoDatos
            servicio={servicio}
            hueco={hueco}
            estilistaElegido={estilistaId !== null}
            enviando={reservar.isPending}
            error={reservar.error}
            alConfirmar={(datos) => reservar.mutate(datos)}
            alVolver={() => setPaso('horario')}
          />
        )}
      </div>
    </>
  )
}
