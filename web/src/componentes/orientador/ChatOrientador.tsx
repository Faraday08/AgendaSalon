import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useEffect, useId, useRef, useState, type FormEvent, type KeyboardEvent } from 'react'
import { Link } from 'react-router-dom'
import { ErrorApi } from '../../api/cliente'
import { apiOrientador } from '../../api/orientador'
import type { Cita } from '../../api/tipos'
import { formatearDia, formatearHora } from '../../utilidades/fechas'

interface Mensaje {
  id: number
  autor: 'cliente' | 'asistente' | 'error'
  texto: string
  cita?: Cita
}

const ClaveConversacion = 'agenda-salon.orientador'
const LongitudMaxima = 500
const Saludo =
  '¡Hola! Cuéntame qué te quieres hacer o cómo te quieres ver, y te ayudo a elegir el servicio y a buscar una hora.'

function leerConversacion(): string | null {
  try {
    return sessionStorage.getItem(ClaveConversacion)
  } catch {
    return null
  }
}

function guardarConversacion(id: string) {
  try {
    sessionStorage.setItem(ClaveConversacion, id)
  } catch {
    // Sin almacenamiento la conversación dura mientras el chat esté abierto.
  }
}

/** Botón flotante y ventana de chat con el asistente. No se muestra si el asistente no está configurado. */
export function ChatOrientador() {
  const id = useId()
  const clienteConsultas = useQueryClient()
  const [abierto, setAbierto] = useState(false)
  const [texto, setTexto] = useState('')
  const [mensajes, setMensajes] = useState<Mensaje[]>([{ id: 0, autor: 'asistente', texto: Saludo }])
  const conversacionId = useRef<string | null>(leerConversacion())
  const siguienteId = useRef(1)
  const lista = useRef<HTMLOListElement>(null)
  const campo = useRef<HTMLTextAreaElement>(null)

  const estado = useQuery({ queryKey: ['orientador', 'estado'], queryFn: apiOrientador.estado, staleTime: 5 * 60_000 })

  const agregar = (mensaje: Omit<Mensaje, 'id'>) =>
    setMensajes((actuales) => [...actuales, { ...mensaje, id: siguienteId.current++ }])

  const enviar = useMutation({
    mutationFn: (mensaje: string) => apiOrientador.enviar(conversacionId.current, mensaje),
    onSuccess: (respuesta) => {
      conversacionId.current = respuesta.conversacionId
      guardarConversacion(respuesta.conversacionId)
      agregar({ autor: 'asistente', texto: respuesta.respuesta, cita: respuesta.citaCreada ?? undefined })
      if (respuesta.citaCreada) void clienteConsultas.invalidateQueries({ queryKey: ['disponibilidad'] })
    },
    onError: (error) =>
      agregar({
        autor: 'error',
        texto: error instanceof ErrorApi ? error.message : 'No pude responder. Inténtalo de nuevo en un momento.',
      }),
  })

  // Baja al último mensaje y devuelve el foco al campo cuando llega la respuesta.
  useEffect(() => {
    lista.current?.lastElementChild?.scrollIntoView({ block: 'end' })
  }, [mensajes, enviar.isPending])

  useEffect(() => {
    if (abierto) campo.current?.focus()
  }, [abierto, enviar.isPending])

  if (!estado.data?.disponible) return null

  const mandar = (e?: FormEvent | KeyboardEvent) => {
    e?.preventDefault()
    const mensaje = texto.trim()
    if (!mensaje || enviar.isPending) return
    agregar({ autor: 'cliente', texto: mensaje })
    setTexto('')
    enviar.mutate(mensaje)
  }

  // Enter envía; Mayús + Enter hace un salto de línea.
  const alTeclear = (e: KeyboardEvent<HTMLTextAreaElement>) => {
    // isComposing: no enviar mientras se escribe con un teclado de acentos o un método de entrada.
    if (e.key === 'Enter' && !e.shiftKey && !e.nativeEvent.isComposing) mandar(e)
    if (e.key === 'Escape') setAbierto(false)
  }

  if (!abierto) {
    return (
      <button type="button" className="boton boton-primario chat-lanzador" onClick={() => setAbierto(true)}>
        <span aria-hidden="true">✦</span> ¿No sabes qué pedir? Pregúntame
      </button>
    )
  }

  return (
    <section className="chat" aria-labelledby={`${id}-titulo`}>
      <header className="chat-cabecera">
        <div>
          <h2 id={`${id}-titulo`}>Asistente del salón</h2>
          <p>Te ayuda a elegir y reservar. Usa IA y puede equivocarse: revisa el resumen antes de confirmar.</p>
        </div>
        <button type="button" className="boton boton-secundario boton-icono" onClick={() => setAbierto(false)} aria-label="Cerrar el chat">
          ✕
        </button>
      </header>

      <ol className="chat-mensajes" ref={lista} aria-live="polite">
        {mensajes.map((m) => (
          <li key={m.id} className={`burbuja burbuja-${m.autor}`}>
            <span className="solo-lectores">{m.autor === 'cliente' ? 'Tú: ' : 'Asistente: '}</span>
            {m.texto}
            {m.cita && <TarjetaCita cita={m.cita} />}
          </li>
        ))}
        {enviar.isPending && (
          <li className="burbuja burbuja-asistente burbuja-escribiendo" aria-label="El asistente está escribiendo">
            <span />
            <span />
            <span />
          </li>
        )}
      </ol>

      <form className="chat-formulario" onSubmit={mandar}>
        <label htmlFor={`${id}-mensaje`} className="solo-lectores">
          Tu mensaje
        </label>
        <textarea
          id={`${id}-mensaje`}
          ref={campo}
          rows={2}
          value={texto}
          maxLength={LongitudMaxima}
          onChange={(e) => setTexto(e.target.value)}
          onKeyDown={alTeclear}
          placeholder="Por ejemplo: quiero un corte el sábado en la mañana"
          disabled={enviar.isPending}
        />
        <button type="submit" className="boton boton-primario" disabled={!texto.trim() || enviar.isPending}>
          Enviar
        </button>
      </form>
    </section>
  )
}

function TarjetaCita({ cita }: { cita: Cita }) {
  return (
    <div className="chat-cita">
      <strong>Cita reservada · {cita.codigo}</strong>
      <span>
        {cita.servicio} con {cita.estilista}
      </span>
      <span>
        {formatearDia(cita.inicio)}, {formatearHora(cita.inicio)}
      </span>
      <Link to={`/mi-cita?codigo=${cita.codigo}`}>Ver o cancelar</Link>
    </div>
  )
}
