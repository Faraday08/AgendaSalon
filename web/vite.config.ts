/// <reference types="vitest/config" />
import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

// En desarrollo, Vite reenvía /api y /hubs a la API de .NET, así el navegador
// lo ve todo en el mismo origen y no hace falta CORS.
const api = process.env.API_URL ?? 'http://localhost:5063'

export default defineConfig({
  // En GitHub Pages el sitio vive en /AgendaSalon/; en desarrollo, en la raíz.
  base: process.env.BASE_PATH ?? '/',
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      '/api': { target: api, changeOrigin: true },
      '/hubs': { target: api, changeOrigin: true, ws: true },
    },
  },
  test: {
    environment: 'jsdom',
    setupFiles: ['./src/pruebas/configuracion.ts'],
    css: false,
  },
})
