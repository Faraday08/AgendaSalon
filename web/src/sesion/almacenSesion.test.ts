import { describe, expect, it } from 'vitest'
import type { Sesion } from '../api/tipos'
import { borrarSesion, guardarSesion, leerSesion } from './almacenSesion'

const sesion = (expiraEnUtc: string): Sesion => ({
  token: 'token-de-prueba',
  expiraEnUtc,
  nombre: 'Administrador',
  rol: 'Administrador',
  estilistaId: null,
})

describe('almacenSesion', () => {
  it('devuelve la sesión guardada mientras no haya expirado', () => {
    guardarSesion(sesion('2026-10-26T22:00:00Z'))

    expect(leerSesion(Date.parse('2026-10-26T21:00:00Z'))?.token).toBe('token-de-prueba')
  })

  it('descarta y borra la sesión expirada', () => {
    guardarSesion(sesion('2026-10-26T22:00:00Z'))

    expect(leerSesion(Date.parse('2026-10-26T22:00:01Z'))).toBeNull()
    expect(localStorage.length).toBe(0)
  })

  it('ignora datos corruptos', () => {
    localStorage.setItem('agenda-salon.sesion', '{no es json')

    expect(leerSesion()).toBeNull()
  })

  it('borrarSesion deja el almacenamiento vacío', () => {
    guardarSesion(sesion('2999-01-01T00:00:00Z'))
    borrarSesion()

    expect(leerSesion()).toBeNull()
  })
})
