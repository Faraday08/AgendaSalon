import { pedir } from './cliente'
import type { Cita } from './tipos'

export interface RespuestaOrientador {
  conversacionId: string
  respuesta: string
  citaCreada: Cita | null
}

export const apiOrientador = {
  estado: () => pedir<{ disponible: boolean }>('/api/orientador/estado'),

  enviar: (conversacionId: string | null, mensaje: string) =>
    pedir<RespuestaOrientador>('/api/orientador/mensajes', { metodo: 'POST', cuerpo: { conversacionId, mensaje } }),
}
