import { pedir } from './cliente'
import type { Servicio } from './tipos'

export interface Recomendacion {
  motivo: string
  servicios: Servicio[]
}

export interface OpcionGuia {
  texto: string
  siguientePregunta: string | null
  recomendacion: Recomendacion | null
}

export interface PreguntaGuia {
  id: string
  texto: string
  opciones: OpcionGuia[]
}

export interface GuiaOrientacion {
  preguntaInicial: string
  preguntas: PreguntaGuia[]
}

export interface Interpretacion {
  entendido: boolean
  mensaje: string
  pregunta: string | null
  recomendacion: Recomendacion | null
}

export const apiOrientacion = {
  guia: () => pedir<GuiaOrientacion>('/api/orientacion/guia'),

  interpretar: (texto: string) => pedir<Interpretacion>('/api/orientacion/interpretar', { metodo: 'POST', cuerpo: { texto } }),
}
