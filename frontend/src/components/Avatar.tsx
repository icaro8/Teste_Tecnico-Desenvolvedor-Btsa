import type { CSSProperties } from 'react'

const HUES = [172, 200, 222, 262, 292, 330, 12, 32, 142]

function initials(name: string) {
  const parts = name.trim().split(/\s+/).filter(Boolean)
  if (parts.length === 0) return '?'
  const first = parts[0][0]
  const last = parts.length > 1 ? parts[parts.length - 1][0] : ''
  return (first + last).toUpperCase()
}

function hueFor(name: string) {
  let hash = 0
  for (const char of name) hash = (hash * 31 + char.charCodeAt(0)) >>> 0
  return HUES[hash % HUES.length]
}

export function Avatar({ name, size = 'md' }: { name: string; size?: 'sm' | 'md' | 'lg' }) {
  return (
    <span className={`avatar avatar--${size}`} style={{ '--avatar-hue': hueFor(name) } as CSSProperties} aria-hidden="true">
      {initials(name)}
    </span>
  )
}
