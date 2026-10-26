import { useQuery } from '@tanstack/react-query'
import { useCallback, useEffect, useMemo, useState } from 'react'
import { useSearchParams } from 'react-router-dom'
import { apiPanel } from '../../api/panel'
import { apiReservas } from '../../api/reservas'
import type { CitaPanel, Estilista, FechaLocal } from '../../api/tipos'
import { Marca } from '../../componentes/Marca'
import { MensajeError } from '../../componentes/MensajeError'
import { useSesion } from '../../sesion/ContextoSesion'
import { claveCitasDelDia } from '../../tiempoReal/clavesConsulta'
import { useAgendaEnVivo, type AvisoCita, type EstadoConexion } from '../../tiempoReal/useAgendaEnVivo'
import { formatearPrecio } from '../../utilidades/dinero'
import { aFechaLocal, fechaDe, formatearDia, formatearDiaCorto, formatearHora, hoy, sumarDias } from '../../utilidades/fechas'
import { CalendarioDia } from './CalendarioDia'
import { DetalleCita } from './DetalleCita'
import { Notificaciones } from './Notificaciones'
import { useNotificaciones } from './useNotificaciones'

const TextoConexion: Record<EstadoConexion, string> = {
  conectando: 'Conectando…',
  'en-vivo': 'En vivo',
  reconectando: 'Reconectando…',
  desconectado: 'Sin conexión en vivo',
}

const fechaValida = (texto: string | null): texto is FechaLocal => texto !== null && /^\d{4}-\d{2}-\d{2}$/.test(texto)

export function PaginaPanel() {
  const { sesion, cerrarSesion } = useSesion()
  const [parametros, setParametros] = useSearchParams()
  const fechaParametro = parametros.get('fecha')
  const fecha = fechaValida(fechaParametro) ? fechaParametro : hoy()
  const irA = (nueva: FechaLocal) => setParametros(nueva === hoy() ? {} : { fecha: nueva })

  const [citaAbierta, setCitaAbierta] = useState<CitaPanel | null>(null)
  const [recientes, setRecientes] = useState<ReadonlySet<string>>(new Set())
  const { lista, notificar } = useNotificaciones()

  const citas = useQuery({
    queryKey: claveCitasDelDia(fecha),
    queryFn: ({ signal }) => apiPanel.citas(fecha, fecha, signal),
  })
  const estilistas = useQuery({ queryKey: ['estilistas'], queryFn: () => apiReservas.estilistas(), staleTime: 5 * 60_000 })

  const alRecibir = useCallback(
    ({ tipo, cita }: AvisoCita) => {
      const cuando = `${formatearDiaCorto(cita.inicio)} ${formatearHora(cita.inicio)}`
      notificar(
        tipo === 'creada'
          ? `Nueva cita: ${cita.clienteNombre}, ${cita.servicio} (${cuando})`
          : `Cita de ${cita.clienteNombre} (${cuando}): ${cita.estado === 'NoAsistio' ? 'no asistió' : cita.estado.toLowerCase()}`,
      )
      setRecientes((actual) => new Set(actual).add(cita.codigo))
      window.setTimeout(
        () =>
          setRecientes((actual) => {
            const siguiente = new Set(actual)
            siguiente.delete(cita.codigo)
            return siguiente
          }),
        3000,
      )
      // Mantiene abierta la ficha con los datos nuevos si es la misma cita.
      setCitaAbierta((abierta) => (abierta?.codigo === cita.codigo ? cita : abierta))
    },
    [notificar, setCitaAbierta],
  )
  const conexion = useAgendaEnVivo(sesion?.token ?? null, alRecibir)

  // Refresca cada minuto la línea roja de "ahora".
  const [ahora, setAhora] = useState(() => new Date())
  useEffect(() => {
    const intervalo = window.setInterval(() => setAhora(new Date()), 60_000)
    return () => window.clearInterval(intervalo)
  }, [])

  const columnas: Estilista[] = useMemo(() => {
    const todos = estilistas.data ?? []
    if (sesion?.rol !== 'Estilista') return todos
    const propio = todos.find((e) => e.id === sesion.estilistaId)
    return propio ? [propio] : []
  }, [estilistas.data, sesion])

  const metricas = useMemo(() => {
    const lista = citas.data ?? []
    const activas = lista.filter((c) => c.estado === 'Confirmada' || c.estado === 'Completada')
    return {
      total: activas.length,
      completadas: lista.filter((c) => c.estado === 'Completada').length,
      canceladas: lista.filter((c) => c.estado === 'Cancelada' || c.estado === 'NoAsistio').length,
      ingresos: activas.reduce((suma, c) => suma + c.precio, 0),
    }
  }, [citas.data])

  if (!sesion) return null
  const esHoy = fecha === aFechaLocal(ahora)

  return (
    <>
      <header className="encabezado">
        <div className="contenedor contenedor-ancho">
          <Marca destino="/panel" />
          <div className="fila">
            <span className={`insignia conexion-${conexion}`} role="status">
              {TextoConexion[conexion]}
            </span>
            <span className="texto-suave">
              {sesion.nombre} · {sesion.rol === 'Administrador' ? 'Administrador' : 'Estilista'}
            </span>
            <button type="button" className="boton boton-secundario boton-pequeno" onClick={cerrarSesion}>
              Salir
            </button>
          </div>
        </div>
      </header>

      <main className="contenedor contenedor-ancho">
        <div className="barra-panel">
          <h1>{formatearDia(fecha)}</h1>
          <div className="navegacion-dias">
            <button type="button" className="boton boton-secundario boton-icono" onClick={() => irA(sumarDias(fecha, -1))} aria-label="Día anterior">
              ‹
            </button>
            <button type="button" className="boton boton-secundario" onClick={() => irA(hoy())} disabled={esHoy}>
              Hoy
            </button>
            <button type="button" className="boton boton-secundario boton-icono" onClick={() => irA(sumarDias(fecha, 1))} aria-label="Día siguiente">
              ›
            </button>
            <label className="solo-lectores" htmlFor="fecha-panel">
              Ir a la fecha
            </label>
            <input id="fecha-panel" type="date" value={fecha} onChange={(e) => e.target.value && irA(e.target.value)} />
          </div>
        </div>

        <section className="metricas" aria-label="Resumen del día">
          <div className="metrica">
            <div className="metrica-valor">{metricas.total}</div>
            <div className="metrica-nombre">Citas</div>
          </div>
          <div className="metrica">
            <div className="metrica-valor">{metricas.completadas}</div>
            <div className="metrica-nombre">Completadas</div>
          </div>
          <div className="metrica">
            <div className="metrica-valor">{metricas.canceladas}</div>
            <div className="metrica-nombre">Canceladas o ausentes</div>
          </div>
          <div className="metrica">
            <div className="metrica-valor">{formatearPrecio(metricas.ingresos)}</div>
            <div className="metrica-nombre">Ingresos previstos</div>
          </div>
        </section>

        {(citas.isPending || estilistas.isPending) && <p className="cargando">Cargando la agenda…</p>}
        <MensajeError error={citas.error ?? estilistas.error} />
        {citas.data && estilistas.data && (
          <CalendarioDia
            fecha={fecha}
            estilistas={columnas}
            citas={citas.data.filter((c) => fechaDe(c.inicio) === fecha)}
            recientes={recientes}
            ahoraMinutos={esHoy ? ahora.getHours() * 60 + ahora.getMinutes() : null}
            alElegirCita={setCitaAbierta}
          />
        )}
      </main>

      <DetalleCita cita={citaAbierta} ahora={ahora} alCerrar={() => setCitaAbierta(null)} />
      <Notificaciones lista={lista} />
    </>
  )
}
