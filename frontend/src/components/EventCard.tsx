import { Link } from 'react-router-dom'
import type { EventItem } from '../api/types'
import { dateParts, formatTime } from '../lib/format'
import { CapacityBar, StatusBadge } from './EventStatus'

export function EventCard({ event }: { event: EventItem }) {
  const { month, day } = dateParts(event.date)

  return (
    <Link to={`/events/${event.id}`} className={`card event-card${event.isPast ? ' event-card--past' : ''}`}>
      <div className="event-card__date" aria-hidden="true">
        <span className="event-card__month">{month}</span>
        <span className="event-card__day">{day}</span>
      </div>
      <div className="event-card__body">
        <div className="event-card__header">
          <h3 className="event-card__title">{event.title}</h3>
          <StatusBadge event={event} />
        </div>
        <p className="event-card__meta">{formatTime(event.date)}</p>
        {event.description && <p className="event-card__description">{event.description}</p>}
        <CapacityBar event={event} />
      </div>
    </Link>
  )
}
