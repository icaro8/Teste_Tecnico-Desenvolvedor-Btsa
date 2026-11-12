import type { Tone } from '../api/types'

export function Badge({ tone, label }: { tone: Tone; label: string }) {
  return (
    <span className={`badge badge--${tone.toLowerCase()}`} data-testid="badge">
      {label}
    </span>
  )
}

export function Loading({ label = 'Carregando…' }: { label?: string }) {
  return (
    <div className="loading" role="status">
      <span className="spinner" aria-hidden="true" />
      <span>{label}</span>
    </div>
  )
}
