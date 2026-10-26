import { pedir } from './cliente'
import type { Cita, CrearCitaSolicitud, Estilista, FechaLocal, HuecoDisponible, Servicio } from './tipos'

export const apiReservas = {
  servicios: () => pedir<Servicio[]>('/api/servicios'),

  estilistas: (servicioId?: number) =>
    pedir<Estilista[]>(servicioId === undefined ? '/api/estilistas' : `/api/estilistas?servicioId=${servicioId}`),

  disponibilidad: (servicioId: number, fecha: FechaLocal, estilistaId: number | null) => {
    const parametros = new URLSearchParams({ servicioId: String(servicioId), fecha })
    if (estilistaId !== null) parametros.set('estilistaId', String(estilistaId))
    return pedir<HuecoDisponible[]>(`/api/disponibilidad?${parametros}`)
  },

  crearCita: (solicitud: CrearCitaSolicitud) => pedir<Cita>('/api/citas', { metodo: 'POST', cuerpo: solicitud }),

  obtenerCita: (codigo: string) => pedir<Cita>(`/api/citas/${encodeURIComponent(codigo.trim())}`),

  cancelarCita: (codigo: string, telefono: string) =>
    pedir<Cita>(`/api/citas/${encodeURIComponent(codigo.trim())}/cancelacion`, { metodo: 'POST', cuerpo: { telefono } }),
}
