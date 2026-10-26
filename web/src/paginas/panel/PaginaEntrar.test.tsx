import { screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { renderizar, simularApi } from '../../pruebas/utilidades'
import { PaginaEntrar } from './PaginaEntrar'

describe('PaginaEntrar', () => {
  afterEach(() => vi.unstubAllGlobals())

  it('con credenciales incorrectas muestra el mensaje de la API y limpia la contraseña', async () => {
    simularApi((url) =>
      url.pathname === '/api/auth/login' ? { estado: 401, cuerpo: { title: 'Correo o contraseña incorrectos.' } } : undefined,
    )
    const usuario = userEvent.setup()
    renderizar(<PaginaEntrar />, '/panel/entrar')

    await usuario.type(screen.getByLabelText('Correo'), 'admin@salon.com')
    await usuario.type(screen.getByLabelText('Contraseña'), 'incorrecta')
    await usuario.click(screen.getByRole('button', { name: 'Entrar' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('Correo o contraseña incorrectos.')
    expect(screen.getByLabelText('Contraseña')).toHaveValue('')
  })

  it('con credenciales correctas guarda la sesión', async () => {
    simularApi((url) =>
      url.pathname === '/api/auth/login'
        ? {
            cuerpo: {
              token: 'token-de-prueba',
              expiraEnUtc: '2999-01-01T00:00:00Z',
              nombre: 'Administrador',
              rol: 'Administrador',
              estilistaId: null,
            },
          }
        : undefined,
    )
    const usuario = userEvent.setup()
    renderizar(<PaginaEntrar />, '/panel/entrar')

    await usuario.type(screen.getByLabelText('Correo'), 'admin@salon.com')
    await usuario.type(screen.getByLabelText('Contraseña'), 'Correcta2026')
    await usuario.click(screen.getByRole('button', { name: 'Entrar' }))

    await vi.waitFor(() => expect(localStorage.getItem('agenda-salon.sesion')).toContain('token-de-prueba'))
  })
})
