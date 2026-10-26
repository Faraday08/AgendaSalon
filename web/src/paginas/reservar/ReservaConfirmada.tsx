import { useEffect, useRef } from 'react'
import { Link } from 'react-router-dom'
import type { Cita } from '../../api/tipos'
import { formatearPrecio } from '../../utilidades/dinero'
import { formatearDia, formatearHora } from '../../utilidades/fechas'

export function ReservaConfirmada({ cita, alReservarOtra }: { cita: Cita; alReservarOtra: () => void }) {
  const titulo = useRef<HTMLHeadingElement>(null)

  // Lleva el foco al título para que los lectores de pantalla anuncien la confirmación.
  useEffect(() => titulo.current?.focus(), [])

  return (
    <section className="pila" aria-labelledby="titulo-confirmada">
      <h2 id="titulo-confirmada" ref={titulo} tabIndex={-1}>
        ¡Listo, {cita.clienteNombre.split(' ')[0]}! Tu cita está reservada
      </h2>
      <p>Guarda este código. Lo necesitas para consultar o cancelar tu cita.</p>
      <p className="codigo-cita" aria-label={`Código de la cita: ${cita.codigo.split('').join(' ')}`}>
        {cita.codigo}
      </p>
      <dl className="resumen tarjeta">
        <dt>Servicio</dt>
        <dd>{cita.servicio}</dd>
        <dt>Día</dt>
        <dd>{formatearDia(cita.inicio)}</dd>
        <dt>Hora</dt>
        <dd>
          {formatearHora(cita.inicio)} a {formatearHora(cita.fin)}
        </dd>
        <dt>Estilista</dt>
        <dd>{cita.estilista}</dd>
        <dt>Precio</dt>
        <dd>{formatearPrecio(cita.precio)}</dd>
      </dl>
      <div className="acciones-paso">
        <Link to={`/mi-cita?codigo=${cita.codigo}`} className="boton boton-secundario">
          Ver o cancelar mi cita
        </Link>
        <button type="button" className="boton boton-primario" onClick={alReservarOtra}>
          Hacer otra reserva
        </button>
      </div>
    </section>
  )
}
