import type { ReactNode } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { ApiError } from '../api/client'
import { useCreateEvent, useEvent, useUpdateEvent } from '../api/hooks'
import type { EventInput } from '../api/types'
import { EventForm } from '../components/EventForm'
import { ErrorState, Spinner } from '../components/Feedback'
import { useToast } from '../components/Toast'
import { NotFoundPage } from './NotFoundPage'

export function CreateEventPage() {
  const navigate = useNavigate()
  const toast = useToast()
  const create = useCreateEvent()

  function handleSubmit(input: EventInput) {
    create.mutate(input, {
      onSuccess: (evt) => {
        toast.success('Event created.')
        navigate(`/events/${evt.id}`)
      },
    })
  }

  return (
    <FormShell title="New event" backTo="/">
      <EventForm submitLabel="Create event" submitting={create.isPending} error={create.error}
        onSubmit={handleSubmit} onCancel={() => navigate(-1)} />
    </FormShell>
  )
}

export function EditEventPage() {
  const { id = '' } = useParams()
  const navigate = useNavigate()
  const toast = useToast()
  const { data: event, isPending, isPlaceholderData, error, refetch } = useEvent(id)
  const update = useUpdateEvent(id)

  // Wait for fresh data: the form is initialised once, so it must not start from a stale copy.
  if (isPending || isPlaceholderData) return <Spinner label="Loading event…" />
  if (error instanceof ApiError && error.status === 404) return <NotFoundPage message="This event doesn't exist or was deleted." />
  if (error) return <ErrorState error={error} onRetry={() => refetch()} />

  if (event.isPast) {
    return (
      <FormShell title="Edit event" backTo={`/events/${id}`}>
        <div className="alert alert--info">Past events can't be edited.</div>
      </FormShell>
    )
  }

  function handleSubmit(input: EventInput) {
    update.mutate(input, {
      onSuccess: () => {
        toast.success('Event updated.')
        navigate(`/events/${id}`)
      },
    })
  }

  return (
    <FormShell title="Edit event" backTo={`/events/${id}`}>
      <EventForm initial={event} submitLabel="Save changes" submitting={update.isPending} error={update.error}
        onSubmit={handleSubmit} onCancel={() => navigate(`/events/${id}`)} />
    </FormShell>
  )
}

function FormShell({ title, backTo, children }: { title: string; backTo: string; children: ReactNode }) {
  return (
    <>
      <Link to={backTo} className="back-link">← Back</Link>
      <section className="card form-card">
        <h1>{title}</h1>
        {children}
      </section>
    </>
  )
}
