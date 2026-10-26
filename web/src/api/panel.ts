import { pedir } from './cliente'
import type { CitaPanel, FechaLocal, Sesion } from './tipos'

export type AccionCita = 'completada' | 'no-asistio' | 'cancelacion'

export const apiPanel = {
  iniciarSesion: (correo: string, contrasena: string) =>
    pedir<Sesion>('/api/auth/login', { metodo: 'POST', cuerpo: { correo, contrasena } }),

  citas: (desde: FechaLocal, hasta: FechaLocal, senal?: AbortSignal) =>
    pedir<CitaPanel[]>(`/api/panel/citas?desde=${desde}&hasta=${hasta}`, { autenticado: true, senal }),

  cambiarEstado: (codigo: string, accion: AccionCita) =>
    pedir<CitaPanel>(`/api/panel/citas/${encodeURIComponent(codigo)}/${accion}`, { metodo: 'POST', autenticado: true }),
}
