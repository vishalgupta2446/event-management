import { useState, type FormEvent } from 'react'
import { ApiError } from '../api/client'
import { useRegister, useRegistrations, useUnregister } from '../api/hooks'
import type { EventItem } from '../api/types'
import { formatShortDate, pluralize } from '../lib/format'
import { ErrorState, Spinner } from './Feedback'
import { useToast } from './Toast'

const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]+$/

export function RegistrationForm({ event }: { event: EventItem }) {
  const toast = useToast()
  const register = useRegister(event.id)
  const [name, setName] = useState('')
  const [email, setEmail] = useState('')
  const [localError, setLocalError] = useState<string | null>(null)

  if (event.isPast) {
    return <div className="alert alert--info">This event has already taken place, so registration is closed.</div>
  }
  if (event.isFull) {
    return <div className="alert alert--warning">This event is full. Spots will open up if someone unregisters.</div>
  }

  const serverError = register.error instanceof ApiError ? register.error : null
  const fieldErrors = serverError?.fieldErrors ?? {}
  const message = localError ?? (serverError && !Object.keys(fieldErrors).length ? serverError.message : null)

  function handleSubmit(e: FormEvent) {
    e.preventDefault()
    if (!name.trim()) return setLocalError('Please enter a name.')
    if (!EMAIL_PATTERN.test(email.trim())) return setLocalError('Please enter a valid email address.')
    setLocalError(null)

    register.mutate(
      { name: name.trim(), email: email.trim() },
      {
        onSuccess: (r) => {
          toast.success(`${r.name} is registered for ${event.title}.`)
          setName('')
          setEmail('')
          register.reset()
        },
      },
    )
  }

  return (
    <form className="form form--compact" onSubmit={handleSubmit} noValidate>
      {message && <div className="alert alert--error" role="alert">{message}</div>}
      <div className="field">
        <label htmlFor="reg-name">Name</label>
        <input id="reg-name" value={name} onChange={(e) => setName(e.target.value)} maxLength={200}
          placeholder="Ada Lovelace" autoComplete="name" />
        {fieldErrors.name && <p className="field__error">{fieldErrors.name}</p>}
      </div>
      <div className="field">
        <label htmlFor="reg-email">Email</label>
        <input id="reg-email" type="email" value={email} onChange={(e) => setEmail(e.target.value)} maxLength={320}
          placeholder="ada@example.com" autoComplete="email" />
        {fieldErrors.email && <p className="field__error">{fieldErrors.email}</p>}
      </div>
      <button type="submit" className="btn btn--primary btn--block" disabled={register.isPending}>
        {register.isPending ? 'Registering…' : 'Register'}
      </button>
    </form>
  )
}

export function AttendeeList({ event }: { event: EventItem }) {
  const toast = useToast()
  const { data, isPending, error, refetch } = useRegistrations(event.id)
  const unregister = useUnregister(event.id)

  if (isPending) return <Spinner label="Loading attendees…" />
  if (error) return <ErrorState error={error} onRetry={() => refetch()} />
  if (!data.length) return <p className="muted">No one has registered yet.</p>

  function handleUnregister(id: string, name: string) {
    if (!window.confirm(`Remove ${name} from ${event.title}?`)) return
    unregister.mutate(id, {
      onSuccess: () => toast.success(`${name} has been unregistered.`),
      onError: (err) => toast.error(err.message),
    })
  }

  return (
    <>
      <p className="muted">{pluralize(data.length, 'attendee')}</p>
      <ul className="attendees">
        {data.map((r) => {
          const removing = unregister.isPending && unregister.variables === r.id
          return (
            <li key={r.id} className="attendee">
              <span className="avatar" aria-hidden="true">{r.name.charAt(0).toUpperCase()}</span>
              <div className="attendee__info">
                <span className="attendee__name">{r.name}</span>
                <span className="attendee__email">{r.email}</span>
              </div>
              <span className="attendee__date muted">{formatShortDate(r.registeredAt)}</span>
              {!event.isPast && (
                <button type="button" className="btn btn--ghost btn--small" disabled={removing}
                  onClick={() => handleUnregister(r.id, r.name)} aria-label={`Unregister ${r.name}`}>
                  {removing ? 'Removing…' : 'Unregister'}
                </button>
              )}
            </li>
          )
        })}
      </ul>
    </>
  )
}
