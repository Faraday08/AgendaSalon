import { screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import type { Cita, CrearCitaSolicitud, Estilista, HuecoDisponible, Servicio } from '../../api/tipos'
import { renderizar, simularApi } from '../../pruebas/utilidades'
import { PaginaReservar } from './PaginaReservar'

const servicios: Servicio[] = [
  { id: 1, nombre: 'Corte de dama', descripcion: 'Corte, lavado y secado.', categoria: 'Corte', duracionMinutos: 45, precio: 800 },
  { id: 2, nombre: 'Manicure', descripcion: 'Limpieza y esmaltado.', categoria: 'Uñas', duracionMinutos: 45, precio: 600 },
]
const estilistas: Estilista[] = [
  { id: 1, nombre: 'Carla Méndez', especialidad: 'Colorista' },
  { id: 3, nombre: 'Yaritza Ramírez', especialidad: 'Uñas y peinados' },
]
const huecos: HuecoDisponible[] = [
  { estilistaId: 1, estilistaNombre: 'Carla Méndez', inicio: '2026-10-29T10:00:00', fin: '2026-10-29T10:45:00' },
  { estilistaId: 3, estilistaNombre: 'Yaritza Ramírez', inicio: '2026-10-29T10:00:00', fin: '2026-10-29T10:45:00' },
  { estilistaId: 1, estilistaNombre: 'Carla Méndez', inicio: '2026-10-29T10:15:00', fin: '2026-10-29T11:00:00' },
]
const citaCreada: Cita = {
  codigo: 'K7PQ2M',
  servicio: 'Corte de dama',
  estilista: 'Carla Méndez',
  inicio: '2026-10-29T10:00:00',
  fin: '2026-10-29T10:45:00',
  estado: 'Confirmada',
  clienteNombre: 'María Rodríguez',
  precio: 800,
}

function apiDelSalon(respuestaReserva: { estado?: number; cuerpo: unknown } = { estado: 201, cuerpo: citaCreada }) {
  return simularApi(
    (url) => (url.pathname === '/api/servicios' ? { cuerpo: servicios } : undefined),
    (url) => (url.pathname === '/api/estilistas' ? { cuerpo: estilistas } : undefined),
    (url) => (url.pathname === '/api/disponibilidad' ? { cuerpo: huecos } : undefined),
    (url, peticion) => (url.pathname === '/api/citas' && peticion?.method === 'POST' ? respuestaReserva : undefined),
  )
}

async function llegarAlPasoDeDatos() {
  const usuario = userEvent.setup()
  renderizar(<PaginaReservar />)
  await usuario.click(await screen.findByRole('button', { name: /Corte de dama/ }))
  const horas = await screen.findByRole('group', { name: 'Horas libres' })
  await usuario.click(within(horas).getByRole('button', { name: '10:00 a. m.' }))
  return usuario
}

describe('PaginaReservar', () => {
  beforeEach(() => {
    // Solo se congela la fecha; los temporizadores siguen reales para React Query y user-event.
    vi.useFakeTimers({ toFake: ['Date'] })
    vi.setSystemTime(new Date(2026, 9, 29, 8, 0))
  })

  afterEach(() => {
    vi.useRealTimers()
    vi.unstubAllGlobals()
  })

  it('muestra los servicios agrupados por categoría con su precio', async () => {
    apiDelSalon()
    renderizar(<PaginaReservar />)

    expect(await screen.findByRole('heading', { name: 'Uñas' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /Corte de dama.*45 min.*RD\$\s?800/ })).toBeInTheDocument()
  })

  it('con un servicio elegido en el asistente guiado empieza directamente en el día y la hora', async () => {
    apiDelSalon()
    renderizar(<PaginaReservar servicioInicial={servicios[0]} />)

    expect(await screen.findByRole('heading', { name: '¿Cuándo te viene bien?' })).toBeInTheDocument()
    expect(screen.getByRole('listitem', { current: 'step' })).toHaveTextContent('Día y hora')
  })

  it('con "cualquier estilista" muestra cada hora una sola vez', async () => {
    apiDelSalon()
    await llegarAlPasoDeDatos()
    await userEvent.setup().click(screen.getByRole('button', { name: 'Cambiar hora' }))

    const horas = await screen.findByRole('group', { name: 'Horas libres' })
    expect(within(horas).getAllByRole('button').map((b) => b.textContent)).toEqual(['10:00 a. m.', '10:15 a. m.'])
  })

  it('reserva de punta a punta y muestra el código de la cita', async () => {
    const api = apiDelSalon()
    const usuario = await llegarAlPasoDeDatos()

    expect(screen.getByText('Carla Méndez')).toBeInTheDocument()
    await usuario.type(screen.getByLabelText('Nombre'), 'María Rodríguez')
    await usuario.type(screen.getByLabelText('Teléfono'), '809-555-1234')
    await usuario.click(screen.getByRole('button', { name: 'Confirmar reserva' }))

    expect(await screen.findByText('K7PQ2M')).toBeInTheDocument()
    const envio = api.mock.calls.find(([, peticion]) => peticion?.method === 'POST')
    expect(JSON.parse(String(envio?.[1]?.body)) as CrearCitaSolicitud).toEqual({
      servicioId: 1,
      estilistaId: null,
      inicio: '2026-10-29T10:00:00',
      clienteNombre: 'María Rodríguez',
      clienteTelefono: '809-555-1234',
      notas: null,
    })
  })

  it('si alguien tomó la hora, vuelve a elegir horario y explica por qué', async () => {
    apiDelSalon({ estado: 409, cuerpo: { title: 'Ese horario no está disponible. Consulta la disponibilidad y elige otra hora.' } })
    const usuario = await llegarAlPasoDeDatos()

    await usuario.type(screen.getByLabelText('Nombre'), 'María Rodríguez')
    await usuario.type(screen.getByLabelText('Teléfono'), '809-555-1234')
    await usuario.click(screen.getByRole('button', { name: 'Confirmar reserva' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('Ese horario no está disponible')
    expect(screen.getByRole('heading', { name: '¿Cuándo te viene bien?' })).toBeInTheDocument()
  })
})
