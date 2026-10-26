import { useMutation, useQuery } from '@tanstack/react-query'
import { useEffect, useId, useRef, useState, type FormEvent } from 'react'
import { useNavigate } from 'react-router-dom'
import { apiOrientacion, type PreguntaGuia, type Recomendacion } from '../../api/orientacion'
import type { Servicio } from '../../api/tipos'
import { formatearPrecio } from '../../utilidades/dinero'
import { formatearDuracion } from '../../utilidades/fechas'
import { MensajeError } from '../MensajeError'

/** Dónde está el cliente dentro de la guía: respondiendo una pregunta o viendo una recomendación. */
type Paso = { tipo: 'pregunta'; id: string; aviso?: string } | { tipo: 'recomendacion'; recomendacion: Recomendacion; aviso?: string }

/**
 * Ayuda a elegir el servicio sin IA: preguntas con opciones y, si el cliente prefiere escribir,
 * reglas de palabras clave en el servidor. Al final lleva a la reserva con el servicio ya elegido.
 */
export function AsistenteGuiado() {
  const id = useId()
  const navegar = useNavigate()
  const [abierto, setAbierto] = useState(false)
  const [pasos, setPasos] = useState<Paso[]>([])
  const [texto, setTexto] = useState('')
  const titulo = useRef<HTMLHeadingElement>(null)

  const guia = useQuery({ queryKey: ['orientacion', 'guia'], queryFn: apiOrientacion.guia, staleTime: 5 * 60_000, enabled: abierto })

  const interpretar = useMutation({
    mutationFn: apiOrientacion.interpretar,
    onSuccess: (resultado) => {
      setTexto('')
      if (resultado.recomendacion) avanzar({ tipo: 'recomendacion', recomendacion: resultado.recomendacion, aviso: resultado.mensaje })
      else if (resultado.pregunta) avanzar({ tipo: 'pregunta', id: resultado.pregunta, aviso: resultado.mensaje })
    },
  })

  // En cada paso nuevo, el foco va al título para que los lectores de pantalla lo anuncien.
  useEffect(() => {
    if (abierto) titulo.current?.focus()
  }, [abierto, pasos.length])

  const avanzar = (paso: Paso) => setPasos((actuales) => [...actuales, paso])
  const volver = () => setPasos((actuales) => actuales.slice(0, -1))
  const cerrar = () => {
    setAbierto(false)
    setPasos([])
    interpretar.reset()
  }

  const reservar = (servicio: Servicio) => {
    cerrar()
    navegar('/', { state: { servicio } })
  }

  const enviarTexto = (e: FormEvent) => {
    e.preventDefault()
    if (texto.trim()) interpretar.mutate(texto.trim())
  }

  if (!abierto) {
    return (
      <button type="button" className="boton boton-primario asistente-lanzador" onClick={() => setAbierto(true)}>
        <span aria-hidden="true">✦</span> ¿No sabes qué pedir?
      </button>
    )
  }

  const pasoActual: Paso | undefined = pasos.at(-1) ?? (guia.data ? { tipo: 'pregunta', id: guia.data.preguntaInicial } : undefined)
  const pregunta: PreguntaGuia | undefined =
    pasoActual?.tipo === 'pregunta' ? guia.data?.preguntas.find((p) => p.id === pasoActual.id) : undefined

  return (
    <section className="asistente" aria-labelledby={`${id}-titulo`} onKeyDown={(e) => e.key === 'Escape' && cerrar()}>
      <header className="asistente-cabecera">
        <span className="asistente-marca">Te ayudo a elegir</span>
        <button type="button" className="boton boton-secundario boton-icono boton-pequeno" onClick={cerrar} aria-label="Cerrar el asistente">
          ✕
        </button>
      </header>

      <div className="asistente-cuerpo">
        {guia.isPending && <p className="cargando">Cargando…</p>}
        <MensajeError error={guia.error} />

        {pasoActual?.aviso && <p className="asistente-aviso">{pasoActual.aviso}</p>}

        {pregunta && (
          <>
            <h2 id={`${id}-titulo`} ref={titulo} tabIndex={-1}>
              {pregunta.texto}
            </h2>
            <div className="asistente-opciones">
              {pregunta.opciones.map((opcion) => (
                <button
                  key={opcion.texto}
                  type="button"
                  className="opcion"
                  onClick={() =>
                    opcion.recomendacion
                      ? avanzar({ tipo: 'recomendacion', recomendacion: opcion.recomendacion })
                      : opcion.siguientePregunta && avanzar({ tipo: 'pregunta', id: opcion.siguientePregunta })
                  }
                >
                  {opcion.texto}
                </button>
              ))}
            </div>
          </>
        )}

        {pasoActual?.tipo === 'recomendacion' && (
          <>
            <h2 id={`${id}-titulo`} ref={titulo} tabIndex={-1}>
              {pasoActual.recomendacion.servicios.length === 1 ? 'Te recomiendo este servicio' : 'Te recomiendo estos servicios'}
            </h2>
            <p className="texto-suave">{pasoActual.recomendacion.motivo}</p>
            <ul className="asistente-recomendaciones">
              {pasoActual.recomendacion.servicios.map((servicio) => (
                <li key={servicio.id} className="tarjeta">
                  <strong>{servicio.nombre}</strong>
                  <span className="texto-suave">{servicio.descripcion}</span>
                  <span className="opcion-detalle">
                    <span>{formatearDuracion(servicio.duracionMinutos)}</span>
                    <span className="opcion-precio">{formatearPrecio(servicio.precio)}</span>
                  </span>
                  <button type="button" className="boton boton-primario boton-pequeno" onClick={() => reservar(servicio)}>
                    Reservar este servicio
                  </button>
                </li>
              ))}
            </ul>
          </>
        )}

        {pasos.length > 0 && (
          <button type="button" className="boton boton-secundario boton-pequeno" onClick={volver}>
            ‹ Volver
          </button>
        )}
      </div>

      <form className="asistente-texto" onSubmit={enviarTexto}>
        <label htmlFor={`${id}-texto`}>O cuéntamelo con tus palabras</label>
        <div className="fila">
          <input
            id={`${id}-texto`}
            value={texto}
            onChange={(e) => setTexto(e.target.value)}
            maxLength={300}
            placeholder="Por ejemplo: me caso el sábado"
            autoComplete="off"
          />
          <button type="submit" className="boton boton-secundario" disabled={!texto.trim() || interpretar.isPending}>
            Buscar
          </button>
        </div>
        <MensajeError error={interpretar.error} />
      </form>
    </section>
  )
}
