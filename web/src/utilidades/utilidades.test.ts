import { describe, expect, it } from 'vitest'
import type { CitaPanel, HuecoDisponible } from '../api/tipos'
import { aplicarCita } from '../tiempoReal/actualizarCitas'
import { formatearPrecio } from './dinero'
import { fechaDe, formatearDuracion, leerFecha, minutosDelDia, sumarDias } from './fechas'
import { horasUnicas } from './horarios'

describe('fechas', () => {
  it('lee la hora local del salón sin desplazarla por la zona horaria', () => {
    const fecha = leerFecha('2026-10-29T10:30:00')

    expect([fecha.getFullYear(), fecha.getMonth(), fecha.getDate(), fecha.getHours(), fecha.getMinutes()]).toEqual([
      2026, 9, 29, 10, 30,
    ])
  })

  it('suma días cruzando meses y años', () => {
    expect(sumarDias('2026-10-31', 1)).toBe('2026-11-01')
    expect(sumarDias('2026-12-31', 1)).toBe('2027-01-01')
    expect(sumarDias('2026-03-01', -1)).toBe('2026-02-28')
  })

  it('calcula los minutos del día y la fecha de una cita', () => {
    expect(minutosDelDia('2026-10-29T10:30:00')).toBe(630)
    expect(fechaDe('2026-10-29T10:30:00')).toBe('2026-10-29')
  })

  it('escribe duraciones legibles', () => {
    expect(formatearDuracion(45)).toBe('45 min')
    expect(formatearDuracion(120)).toBe('2 h')
    expect(formatearDuracion(150)).toBe('2 h 30 min')
  })
})

describe('formatearPrecio', () => {
  it('muestra pesos dominicanos sin decimales', () => {
    expect(formatearPrecio(2800)).toMatch(/^RD\$\s?2,800$/)
  })
})

describe('horasUnicas', () => {
  const hueco = (inicio: string, estilistaNombre: string): HuecoDisponible => ({
    estilistaId: estilistaNombre.length,
    estilistaNombre,
    inicio,
    fin: inicio,
  })

  it('deja cada hora una sola vez, con el primer estilista que la ofrece', () => {
    const resultado = horasUnicas([
      hueco('2026-10-29T09:00:00', 'Carla'),
      hueco('2026-10-29T09:00:00', 'Yaritza'),
      hueco('2026-10-29T09:15:00', 'Yaritza'),
    ])

    expect(resultado.map((h) => [h.inicio.slice(11, 16), h.estilistaNombre])).toEqual([
      ['09:00', 'Carla'],
      ['09:15', 'Yaritza'],
    ])
  })
})

describe('aplicarCita', () => {
  const cita = (codigo: string, inicio: string, estado: CitaPanel['estado'] = 'Confirmada'): CitaPanel => ({
    codigo,
    inicio,
    fin: inicio,
    estado,
    servicioId: 1,
    servicio: 'Corte',
    estilistaId: 1,
    estilista: 'Carla',
    clienteNombre: 'María',
    clienteTelefono: '809-555-1234',
    precio: 800,
    notas: null,
  })

  it('agrega una cita nueva en su lugar según la hora', () => {
    const lista = [cita('AAAAAA', '2026-10-29T09:00:00'), cita('CCCCCC', '2026-10-29T11:00:00')]

    const resultado = aplicarCita(lista, cita('BBBBBB', '2026-10-29T10:00:00'))

    expect(resultado.map((c) => c.codigo)).toEqual(['AAAAAA', 'BBBBBB', 'CCCCCC'])
  })

  it('reemplaza la cita existente sin duplicarla ni modificar la lista original', () => {
    const lista = [cita('AAAAAA', '2026-10-29T09:00:00')]

    const resultado = aplicarCita(lista, cita('AAAAAA', '2026-10-29T09:00:00', 'Cancelada'))

    expect(resultado).toHaveLength(1)
    expect(resultado[0]?.estado).toBe('Cancelada')
    expect(lista[0]?.estado).toBe('Confirmada')
  })
})
