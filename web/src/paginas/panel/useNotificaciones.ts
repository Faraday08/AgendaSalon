import { useCallback, useEffect, useRef, useState } from 'react'

export interface Notificacion {
  id: number
  texto: string
}

const DuracionMs = 6000
const MaximoVisibles = 4

/** Lista de avisos temporales: cada uno desaparece solo a los pocos segundos. */
export function useNotificaciones() {
  const [lista, setLista] = useState<Notificacion[]>([])
  const siguienteId = useRef(0)
  const temporizadores = useRef(new Set<number>())

  useEffect(() => {
    const pendientes = temporizadores.current
    return () => pendientes.forEach((t) => window.clearTimeout(t))
  }, [])

  const notificar = useCallback((texto: string) => {
    const id = ++siguienteId.current
    setLista((actual) => [...actual.slice(-(MaximoVisibles - 1)), { id, texto }])
    const temporizador = window.setTimeout(() => {
      temporizadores.current.delete(temporizador)
      setLista((actual) => actual.filter((n) => n.id !== id))
    }, DuracionMs)
    temporizadores.current.add(temporizador)
  }, [])

  return { lista, notificar }
}
