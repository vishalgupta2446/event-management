import type { ReactNode } from 'react'

export function Spinner({ label = 'Loading…' }: { label?: string }) {
  return (
    <div className="feedback" role="status">
      <span className="spinner" aria-hidden="true" />
      <span>{label}</span>
    </div>
  )
}

export function ErrorState({ error, onRetry }: { error: Error; onRetry?: () => void }) {
  return (
    <div className="feedback feedback--error" role="alert">
      <p>{error.message}</p>
      {onRetry && (
        <button type="button" className="btn btn--secondary" onClick={onRetry}>
          Try again
        </button>
      )}
    </div>
  )
}

export function EmptyState({ title, children }: { title: string; children?: ReactNode }) {
  return (
    <div className="empty">
      <div className="empty__icon" aria-hidden="true">📭</div>
      <h3>{title}</h3>
      {children}
    </div>
  )
}
