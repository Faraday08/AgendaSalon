// Tipos que reflejan los DTO de la API (AgendaSalon.Application).
// Las fechas llegan como texto "AAAA-MM-DDTHH:mm:ss" en hora local del salón, sin zona horaria.

export type FechaHoraLocal = string
export type FechaLocal = string

export type EstadoCita = 'Confirmada' | 'Cancelada' | 'Completada' | 'NoAsistio'
export type RolUsuario = 'Administrador' | 'Estilista'

export interface Servicio {
  id: number
  nombre: string
  descripcion: string
  categoria: string
  duracionMinutos: number
  precio: number
}

export interface Estilista {
  id: number
  nombre: string
  especialidad: string
}

export interface HuecoDisponible {
  estilistaId: number
  estilistaNombre: string
  inicio: FechaHoraLocal
  fin: FechaHoraLocal
}

export interface CrearCitaSolicitud {
  servicioId: number
  estilistaId: number | null
  inicio: FechaHoraLocal
  clienteNombre: string
  clienteTelefono: string
  notas: string | null
}

export interface Cita {
  codigo: string
  servicio: string
  estilista: string
  inicio: FechaHoraLocal
  fin: FechaHoraLocal
  estado: EstadoCita
  clienteNombre: string
  precio: number
}

export interface CitaPanel {
  codigo: string
  inicio: FechaHoraLocal
  fin: FechaHoraLocal
  estado: EstadoCita
  servicioId: number
  servicio: string
  estilistaId: number
  estilista: string
  clienteNombre: string
  clienteTelefono: string
  precio: number
  notas: string | null
}

export interface Sesion {
  token: string
  expiraEnUtc: string
  nombre: string
  rol: RolUsuario
  estilistaId: number | null
}

export const NombreEstado: Record<EstadoCita, string> = {
  Confirmada: 'Confirmada',
  Cancelada: 'Cancelada',
  Completada: 'Completada',
  NoAsistio: 'No asistió',
}
