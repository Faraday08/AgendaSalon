import { screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { useLocation } from 'react-router-dom'
import type { GuiaOrientacion, Interpretacion } from '../../api/orientacion'
import type { Servicio } from '../../api/tipos'
import { renderizar, simularApi } from '../../pruebas/utilidades'
import { AsistenteGuiado } from './AsistenteGuiado'

const tinte: Servicio = { id: 5, nombre: 'Tinte completo', descripcion: 'Un solo tono.', categoria: 'Color', duracionMinutos: 120, precio: 2800 }
const peinado: Servicio = { id: 8, nombre: 'Peinado para evento', descripcion: 'Recogido u ondas.', categoria: 'Peinado', duracionMinutos: 60, precio: 1500 }

const guia: GuiaOrientacion = {
  preguntaInicial: 'inicio',
  preguntas: [
    {
      id: 'inicio',
      texto: '¿Qué te gustaría hacerte?',
      opciones: [{ texto: 'Cambiar el color', siguientePregunta: 'color', recomendacion: null }],
    },
    {
      id: 'color',
      texto: '¿Qué estás buscando?',
      opciones: [
        {
          texto: 'Un solo tono o cubrir canas',
          siguientePregunta: null,
          recomendacion: { motivo: 'Es lo indicado para cubrir canas.', servicios: [tinte] },
        },
      ],
    },
  ],
}

const interpretacion: Interpretacion = {
  entendido: true,
  mensaje: 'Esto es lo que te recomiendo:',
  pregunta: null,
  recomendacion: { motivo: 'Para que te dure toda la celebración.', servicios: [peinado] },
}

/** Muestra el servicio que llegó a la página de reservas por el estado de la navegación. */
function ServicioRecibido() {
  const estado = useLocation().state as { servicio?: Servicio } | null
  return <output aria-label="Servicio recibido">{estado?.servicio?.nombre ?? ''}</output>
}

function apiDelSalon() {
  return simularApi(
    (url) => (url.pathname === '/api/orientacion/guia' ? { cuerpo: guia } : undefined),
    (url) => (url.pathname === '/api/orientacion/interpretar' ? { cuerpo: interpretacion } : undefined),
  )
}

describe('AsistenteGuiado', () => {
  afterEach(() => vi.unstubAllGlobals())

  it('guía con preguntas hasta la recomendación y lleva a reservar ese servicio', async () => {
    apiDelSalon()
    const usuario = userEvent.setup()
    renderizar(
      <>
        <AsistenteGuiado />
        <ServicioRecibido />
      </>,
    )

    await usuario.click(screen.getByRole('button', { name: /No sabes qué pedir/ }))
    expect(await screen.findByRole('heading', { name: '¿Qué te gustaría hacerte?' })).toBeInTheDocument()

    await usuario.click(screen.getByRole('button', { name: 'Cambiar el color' }))
    await usuario.click(screen.getByRole('button', { name: 'Un solo tono o cubrir canas' }))

    expect(screen.getByRole('heading', { name: 'Te recomiendo este servicio' })).toBeInTheDocument()
    expect(screen.getByText('Es lo indicado para cubrir canas.')).toBeInTheDocument()
    expect(screen.getByText('Tinte completo')).toBeInTheDocument()

    await usuario.click(screen.getByRole('button', { name: 'Reservar este servicio' }))

    expect(screen.getByLabelText('Servicio recibido')).toHaveTextContent('Tinte completo')
    expect(screen.queryByRole('heading', { name: /Te recomiendo/ })).not.toBeInTheDocument()
  })

  it('el botón Volver regresa a la pregunta anterior', async () => {
    apiDelSalon()
    const usuario = userEvent.setup()
    renderizar(<AsistenteGuiado />)

    await usuario.click(screen.getByRole('button', { name: /No sabes qué pedir/ }))
    await usuario.click(await screen.findByRole('button', { name: 'Cambiar el color' }))
    await usuario.click(screen.getByRole('button', { name: '‹ Volver' }))

    expect(screen.getByRole('heading', { name: '¿Qué te gustaría hacerte?' })).toBeInTheDocument()
  })

  it('con texto libre muestra la recomendación que devuelve el servidor', async () => {
    const api = apiDelSalon()
    const usuario = userEvent.setup()
    renderizar(<AsistenteGuiado />)

    await usuario.click(screen.getByRole('button', { name: /No sabes qué pedir/ }))
    await usuario.type(screen.getByLabelText('O cuéntamelo con tus palabras'), 'me caso el sábado{Enter}')

    expect(await screen.findByText('Peinado para evento')).toBeInTheDocument()
    expect(screen.getByText('Esto es lo que te recomiendo:')).toBeInTheDocument()
    const envio = api.mock.calls.find(([, peticion]) => peticion?.method === 'POST')
    expect(JSON.parse(String(envio?.[1]?.body))).toEqual({ texto: 'me caso el sábado' })
  })
})
