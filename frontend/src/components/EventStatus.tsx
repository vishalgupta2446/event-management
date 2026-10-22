import type { EventItem } from '../api/types'
import { pluralize } from '../lib/format'

export function StatusBadge({ event }: { event: EventItem }) {
  if (event.isPast) return <span className="badge badge--muted">Past</span>
  if (event.isFull) return <span className="badge badge--danger">Full</span>
  if (event.remainingCapacity <= Math.max(3, event.maxCapacity * 0.1))
    return <span className="badge badge--warning">{event.remainingCapacity} left</span>
  return <span className="badge badge--success">Open</span>
}

export function CapacityBar({ event }: { event: EventItem }) {
  const percent = Math.min(100, Math.round((event.registeredCount / event.maxCapacity) * 100))
  const tone = event.isFull ? 'danger' : percent >= 80 ? 'warning' : 'ok'

  return (
    <div className="capacity">
      <div className="capacity__label">
        <span>
          <strong>{event.registeredCount}</strong> / {event.maxCapacity} registered
        </span>
        {!event.isPast && !event.isFull && <span className="muted">{pluralize(event.remainingCapacity, 'spot')} left</span>}
      </div>
      <div
        className="capacity__track"
        role="progressbar"
        aria-valuemin={0}
        aria-valuemax={event.maxCapacity}
        aria-valuenow={event.registeredCount}
        aria-label="Registrations"
      >
        <div className={`capacity__fill capacity__fill--${tone}`} style={{ width: `${percent}%` }} />
      </div>
    </div>
  )
}
