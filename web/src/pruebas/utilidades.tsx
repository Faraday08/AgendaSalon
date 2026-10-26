import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render } from '@testing-library/react'
import type { ReactNode } from 'react'
import { MemoryRouter } from 'react-router-dom'
import { vi } from 'vitest'
import { ProveedorSesion } from '../sesion/ContextoSesion'

/** Renderiza con los mismos proveedores que la aplicación real, sin reintentos para que las pruebas sean rápidas. */
export function renderizar(contenido: ReactNode, ruta = '/') {
  const clienteConsultas = new QueryClient({ defaultOptions: { queries: { retry: false }, mutations: { retry: false } } })
  return render(
    <QueryClientProvider client={clienteConsultas}>
      <MemoryRouter initialEntries={[ruta]}>
        <ProveedorSesion>{contenido}</ProveedorSesion>
      </MemoryRouter>
    </QueryClientProvider>,
  )
}

type Respuesta = { estado?: number; cuerpo: unknown }
type Manejador = (url: URL, peticion: RequestInit | undefined) => Respuesta | undefined

/**
 * Sustituye fetch por una API falsa: cada manejador decide si responde a la URL pedida.
 * Devuelve el mock para revisar qué peticiones se hicieron.
 */
export function simularApi(...manejadores: Manejador[]) {
  const mock = vi.fn(async (entrada: RequestInfo | URL, peticion?: RequestInit) => {
    const url = new URL(String(entrada), 'http://localhost')
    for (const manejador of manejadores) {
      const respuesta = manejador(url, peticion)
      if (respuesta) {
        return new Response(JSON.stringify(respuesta.cuerpo), {
          status: respuesta.estado ?? 200,
          headers: { 'Content-Type': 'application/json' },
        })
      }
    }
    return new Response(JSON.stringify({ title: `Ruta no simulada: ${url.pathname}` }), { status: 404 })
  })
  vi.stubGlobal('fetch', mock)
  return mock
}
