import { screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, describe, expect, it, vi } from 'vitest'
import type { RespuestaOrientador } from '../../api/orientador'
import { renderizar, simularApi } from '../../pruebas/utilidades'
import { ChatOrientador } from './ChatOrientador'

const respuestaConCita: RespuestaOrientador = {
  conversacionId: 'c0a8e1d2-0000-4000-8000-000000000001',
  respuesta: '¡Listo! Tu cita quedó reservada. Guarda el código K7PQ2M.',
  citaCreada: {
    codigo: 'K7PQ2M',
    servicio: 'Corte de dama',
    estilista: 'Carla Méndez',
    inicio: '2026-10-29T10:00:00',
    fin: '2026-10-29T10:45:00',
    estado: 'Confirmada',
    clienteNombre: 'María Rodríguez',
    precio: 800,
  },
}

describe('ChatOrientador', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
    sessionStorage.clear()
  })

  it('no se muestra si el asistente no está disponible', async () => {
    const api = simularApi((url) => (url.pathname === '/api/orientador/estado' ? { cuerpo: { disponible: false } } : undefined))
    renderizar(<ChatOrientador />)

    await vi.waitFor(() => expect(api).toHaveBeenCalled())
    expect(screen.queryByRole('button', { name: /Pregúntame/ })).not.toBeInTheDocument()
  })

  it('envía el mensaje, muestra la respuesta y la cita reservada, y recuerda la conversación', async () => {
    const api = simularApi(
      (url) => (url.pathname === '/api/orientador/estado' ? { cuerpo: { disponible: true } } : undefined),
      (url) => (url.pathname === '/api/orientador/mensajes' ? { cuerpo: respuestaConCita } : undefined),
    )
    const usuario = userEvent.setup()
    renderizar(<ChatOrientador />)

    await usuario.click(await screen.findByRole('button', { name: /Pregúntame/ }))
    await usuario.type(screen.getByLabelText('Tu mensaje'), 'Sí, confirmo{Enter}')

    expect(await screen.findByText(/Tu cita quedó reservada/)).toBeInTheDocument()
    expect(screen.getByText('Cita reservada · K7PQ2M')).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Ver o cancelar' })).toHaveAttribute('href', '/mi-cita?codigo=K7PQ2M')
    expect(sessionStorage.getItem('agenda-salon.orientador')).toBe(respuestaConCita.conversacionId)

    const envio = api.mock.calls.find(([, peticion]) => peticion?.method === 'POST')
    expect(JSON.parse(String(envio?.[1]?.body))).toEqual({ conversacionId: null, mensaje: 'Sí, confirmo' })
  })

  it('si la API falla, muestra el mensaje de error dentro del chat', async () => {
    simularApi(
      (url) => (url.pathname === '/api/orientador/estado' ? { cuerpo: { disponible: true } } : undefined),
      (url) =>
        url.pathname === '/api/orientador/mensajes'
          ? { estado: 503, cuerpo: { title: 'El asistente no está disponible en este momento.' } }
          : undefined,
    )
    const usuario = userEvent.setup()
    renderizar(<ChatOrientador />)

    await usuario.click(await screen.findByRole('button', { name: /Pregúntame/ }))
    await usuario.type(screen.getByLabelText('Tu mensaje'), 'Hola{Enter}')

    expect(await screen.findByText('El asistente no está disponible en este momento.')).toBeInTheDocument()
  })
})
