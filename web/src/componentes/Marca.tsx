import { Link } from 'react-router-dom'

export function Marca({ destino = '/' }: { destino?: string }) {
  return (
    <Link to={destino} className="marca">
      <svg viewBox="0 0 32 32" aria-hidden="true">
        <rect width="32" height="32" rx="8" fill="var(--color-primario)" />
        <path
          d="M10 22l12-12M12.5 10a2.5 2.5 0 1 1-5 0 2.5 2.5 0 0 1 5 0zM24.5 22a2.5 2.5 0 1 1-5 0 2.5 2.5 0 0 1 5 0zM10 10l12 12"
          stroke="var(--color-sobre-primario)"
          strokeWidth="2"
          fill="none"
          strokeLinecap="round"
        />
      </svg>
      Agenda Salón
    </Link>
  )
}
