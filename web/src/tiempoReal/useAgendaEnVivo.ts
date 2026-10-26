import { HubConnectionBuilder, HubConnectionState, LogLevel, type HubConnection } from '@microsoft/signalr'
import { useQueryClient } from '@tanstack/react-query'
import { useEffect, useRef, useState } from 'react'
import { urlApi } from '../api/cliente'
import type { CitaPanel } from '../api/tipos'
import { fechaDe } from '../utilidades/fechas'
import { aplicarCita } from './actualizarCitas'
import { claveCitasDelDia } from './clavesConsulta'

export type EstadoConexion = 'conectando' | 'en-vivo' | 'reconectando' | 'desconectado'
export type TipoAviso = 'creada' | 'actualizada'

export interface AvisoCita {
  tipo: TipoAviso
  cita: CitaPanel
}

/**
 * Conecta el panel al hub /hubs/agenda. Cada cita que llega se aplica directamente a la caché
 * del día al que pertenece, así el calendario se actualiza sin volver a pedir datos.
 * Al reconectar se recargan las agendas por si se perdió algún aviso.
 */
export function useAgendaEnVivo(token: string | null, alRecibir?: (aviso: AvisoCita) => void) {
  const clienteConsultas = useQueryClient()
  const [estado, setEstado] = useState<EstadoConexion>(token ? 'conectando' : 'desconectado')
  // Se guarda la última versión del callback sin reiniciar la conexión cada vez que cambia.
  const alRecibirActual = useRef(alRecibir)
  useEffect(() => {
    alRecibirActual.current = alRecibir
  })

  useEffect(() => {
    if (!token) return

    const conexion: HubConnection = new HubConnectionBuilder()
      .withUrl(urlApi('/hubs/agenda'), { accessTokenFactory: () => token })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build()

    const recibir = (tipo: TipoAviso) => (cita: CitaPanel) => {
      clienteConsultas.setQueryData<CitaPanel[]>(claveCitasDelDia(fechaDe(cita.inicio)), (actuales) =>
        actuales === undefined ? undefined : aplicarCita(actuales, cita),
      )
      alRecibirActual.current?.({ tipo, cita })
    }
    conexion.on('CitaCreada', recibir('creada'))
    conexion.on('CitaActualizada', recibir('actualizada'))
    conexion.onreconnecting(() => setEstado('reconectando'))
    conexion.onreconnected(() => {
      setEstado('en-vivo')
      void clienteConsultas.invalidateQueries({ queryKey: claveCitasDelDia() })
    })
    conexion.onclose(() => setEstado('desconectado'))

    let cancelado = false
    const arranque = conexion
      .start()
      .then(() => {
        if (!cancelado) setEstado('en-vivo')
      })
      .catch(() => {
        if (!cancelado) setEstado('desconectado')
      })

    return () => {
      cancelado = true
      // Se espera a que termine de arrancar antes de detenerla: cortar la negociación a medias
      // deja un error en la consola (pasa siempre en desarrollo, porque StrictMode monta dos veces).
      void arranque.finally(() => {
        if (conexion.state !== HubConnectionState.Disconnected) void conexion.stop()
      })
    }
  }, [token, clienteConsultas])

  return estado
}
