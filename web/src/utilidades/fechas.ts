import type { FechaHoraLocal, FechaLocal } from '../api/tipos'

// La API trabaja con la hora local del salón y sin zona horaria ("2026-10-29T10:00:00").
// Para no desplazar horas al convertir entre zonas, aquí se trabaja con los componentes del texto
// y los objetos Date solo se usan para dar formato.

const dosDigitos = (n: number) => String(n).padStart(2, '0')

export function aFechaLocal(fecha: Date): FechaLocal {
  return `${fecha.getFullYear()}-${dosDigitos(fecha.getMonth() + 1)}-${dosDigitos(fecha.getDate())}`
}

export function hoy(): FechaLocal {
  return aFechaLocal(new Date())
}

/** Convierte "AAAA-MM-DD" o "AAAA-MM-DDTHH:mm[:ss]" en un Date local con esos mismos valores. */
export function leerFecha(texto: FechaLocal | FechaHoraLocal): Date {
  const [fecha = '', hora = '00:00'] = texto.split('T')
  const [anio = 0, mes = 1, dia = 1] = fecha.split('-').map(Number)
  const [horas = 0, minutos = 0] = hora.split(':').map(Number)
  return new Date(anio, mes - 1, dia, horas, minutos)
}

export function sumarDias(fecha: FechaLocal, dias: number): FechaLocal {
  const resultado = leerFecha(fecha)
  resultado.setDate(resultado.getDate() + dias)
  return aFechaLocal(resultado)
}

/** Parte de la fecha de un "AAAA-MM-DDTHH:mm:ss". */
export function fechaDe(fechaHora: FechaHoraLocal): FechaLocal {
  return fechaHora.slice(0, 10)
}

/** Minutos transcurridos desde la medianoche, por ejemplo 10:30 → 630. */
export function minutosDelDia(fechaHora: FechaHoraLocal): number {
  const fecha = leerFecha(fechaHora)
  return fecha.getHours() * 60 + fecha.getMinutes()
}

const formatoHora = new Intl.DateTimeFormat('es-DO', { hour: 'numeric', minute: '2-digit' })
const formatoDia = new Intl.DateTimeFormat('es-DO', { weekday: 'long', day: 'numeric', month: 'long' })
const formatoDiaCorto = new Intl.DateTimeFormat('es-DO', { weekday: 'short', day: 'numeric', month: 'short' })

export const formatearHora = (fechaHora: FechaHoraLocal) => formatoHora.format(leerFecha(fechaHora))

export const formatearDia = (fecha: FechaLocal | FechaHoraLocal) => capitalizar(formatoDia.format(leerFecha(fecha)))

export const formatearDiaCorto = (fecha: FechaLocal | FechaHoraLocal) => capitalizar(formatoDiaCorto.format(leerFecha(fecha)))

export function formatearDuracion(minutos: number): string {
  const horas = Math.floor(minutos / 60)
  const resto = minutos % 60
  if (horas === 0) return `${resto} min`
  return resto === 0 ? `${horas} h` : `${horas} h ${resto} min`
}

function capitalizar(texto: string) {
  return texto.charAt(0).toUpperCase() + texto.slice(1)
}
