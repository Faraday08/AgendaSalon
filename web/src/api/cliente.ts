// Cliente HTTP mínimo sobre fetch. Convierte las respuestas ProblemDetails de la API en ErrorApi
// con el mensaje en español que ya viene listo para mostrar.

import { leerSesion } from '../sesion/almacenSesion'

const urlBase = (import.meta.env.VITE_API_URL as string | undefined)?.replace(/\/$/, '') ?? ''

export class ErrorApi extends Error {
  readonly estado: number

  constructor(estado: number, mensaje: string) {
    super(mensaje)
    this.name = 'ErrorApi'
    this.estado = estado
  }
}

interface ProblemDetails {
  title?: string
  errors?: Record<string, string[]>
}

let alNoAutorizado: () => void = () => {}

/** La sesión registra aquí qué hacer si la API rechaza el token (401). */
export function alRechazarToken(accion: () => void) {
  alNoAutorizado = accion
}

export function urlApi(ruta: string) {
  return `${urlBase}${ruta}`
}

interface Opciones {
  metodo?: 'GET' | 'POST'
  cuerpo?: unknown
  autenticado?: boolean
  senal?: AbortSignal
}

export async function pedir<T>(ruta: string, { metodo = 'GET', cuerpo, autenticado = false, senal }: Opciones = {}): Promise<T> {
  const encabezados: Record<string, string> = { Accept: 'application/json' }
  if (cuerpo !== undefined) encabezados['Content-Type'] = 'application/json'

  const token = autenticado ? leerSesion()?.token : null
  if (token) encabezados.Authorization = `Bearer ${token}`

  let respuesta: Response
  try {
    respuesta = await fetch(urlApi(ruta), {
      method: metodo,
      headers: encabezados,
      body: cuerpo === undefined ? undefined : JSON.stringify(cuerpo),
      signal: senal,
    })
  } catch (error) {
    if (error instanceof DOMException && error.name === 'AbortError') throw error
    throw new ErrorApi(0, 'No pudimos conectar con el salón. Revisa tu conexión e inténtalo de nuevo.')
  }

  if (respuesta.ok) {
    return respuesta.status === 204 ? (undefined as T) : ((await respuesta.json()) as T)
  }

  if (respuesta.status === 401 && autenticado) alNoAutorizado()
  throw new ErrorApi(respuesta.status, await leerMensaje(respuesta))
}

async function leerMensaje(respuesta: Response): Promise<string> {
  if (respuesta.status === 429) return 'Demasiados intentos. Espera un minuto y vuelve a intentarlo.'
  try {
    const problema = (await respuesta.json()) as ProblemDetails
    const primerError = problema.errors ? Object.values(problema.errors)[0]?.[0] : undefined
    if (problema.title && !problema.title.startsWith('One or more')) return problema.title
    if (primerError) return primerError
  } catch {
    // La respuesta no traía JSON.
  }
  return 'Ocurrió un error inesperado. Inténtalo de nuevo.'
}
