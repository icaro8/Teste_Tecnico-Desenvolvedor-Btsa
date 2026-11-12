import type { ReactNode } from 'react'

export function Notice({ tone, title, children }: { tone: 'danger' | 'success' | 'info'; title?: string; children?: ReactNode }) {
  return (
    <div className={`notice notice--${tone}`} role={tone === 'danger' ? 'alert' : 'status'}>
      {title && <strong className="notice__title">{title}</strong>}
      {children && <div>{children}</div>}
    </div>
  )
}

export function ErrorNotice({ error }: { error: Error | null }) {
  if (!error) return null
  const messages = 'messages' in error ? (error.messages as string[]) : [error.message]
  return (
    <Notice tone="danger">
      {messages.map((message) => (
        <p key={message}>{message}</p>
      ))}
    </Notice>
  )
}
