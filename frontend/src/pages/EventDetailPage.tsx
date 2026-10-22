import { Link, useNavigate, useParams } from 'react-router-dom'
import { ApiError } from '../api/client'
import { useDeleteEvent, useEvent } from '../api/hooks'
import { CapacityBar, StatusBadge } from '../components/EventStatus'
import { ErrorState, Spinner } from '../components/Feedback'
import { AttendeeList, RegistrationForm } from '../components/Registrations'
import { useToast } from '../components/Toast'
import { formatDateTime } from '../lib/format'
import { NotFoundPage } from './NotFoundPage'

export function EventDetailPage() {
  const { id = '' } = useParams()
  const navigate = useNavigate()
  const toast = useToast()
  const { data: event, isPending, error, refetch } = useEvent(id)
  const deleteEvent = useDeleteEvent()

  if (isPending) return <Spinner label="Loading event…" />
  if (error instanceof ApiError && error.status === 404) return <NotFoundPage message="This event doesn't exist or was deleted." />
  if (error) return <ErrorState error={error} onRetry={() => refetch()} />

  function handleDelete() {
    if (!window.confirm(`Delete "${event!.title}"? This also removes all of its registrations.`)) return
    deleteEvent.mutate(event!.id, {
      onSuccess: () => {
        toast.success('Event deleted.')
        navigate('/', { replace: true })
      },
      onError: (err) => toast.error(err.message),
    })
  }

  return (
    <>
      <Link to="/" className="back-link">← All events</Link>

      <div className="detail">
        <section className="card detail__main">
          <div className="detail__header">
            <div>
              <StatusBadge event={event} />
              <h1 className="detail__title">{event.title}</h1>
              <p className="detail__date">
                <time dateTime={event.date}>{formatDateTime(event.date)}</time>
              </p>
            </div>
            <div className="detail__actions">
              {!event.isPast && (
                <Link to={`/events/${event.id}/edit`} className="btn btn--secondary btn--small">Edit</Link>
              )}
              <button type="button" className="btn btn--danger btn--small" onClick={handleDelete}
                disabled={deleteEvent.isPending}>
                {deleteEvent.isPending ? 'Deleting…' : 'Delete'}
              </button>
            </div>
          </div>

          {event.description ? (
            <p className="detail__description">{event.description}</p>
          ) : (
            <p className="muted">No description provided.</p>
          )}

          <CapacityBar event={event} />

          <h2 className="section-title">Attendees</h2>
          <AttendeeList event={event} />
        </section>

        <aside className="card detail__side">
          <h2 className="section-title">Register</h2>
          <RegistrationForm event={event} />
        </aside>
      </div>
    </>
  )
}
