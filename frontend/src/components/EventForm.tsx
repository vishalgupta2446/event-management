import { useState, type FormEvent, type ReactNode } from 'react'
import { ApiError } from '../api/client'
import type { EventInput, EventItem } from '../api/types'
import { fromLocalInputValue, toLocalInputValue } from '../lib/format'

interface Props {
  initial?: EventItem
  submitLabel: string
  submitting: boolean
  error: Error | null
  onSubmit: (input: EventInput) => void
  onCancel: () => void
}

type Fields = { title: string; description: string; date: string; maxCapacity: string }
type FieldErrors = Partial<Record<keyof Fields, string>>

function validate(f: Fields, minCapacity: number): FieldErrors {
  const errors: FieldErrors = {}
  if (!f.title.trim()) errors.title = 'Title is required.'
  else if (f.title.trim().length > 200) errors.title = 'Title must be 200 characters or fewer.'
  if (f.description.length > 4000) errors.description = 'Description must be 4000 characters or fewer.'
  if (!f.date) errors.date = 'Date and time are required.'
  else if (new Date(f.date) <= new Date()) errors.date = 'Date must be in the future.'
  const capacity = Number(f.maxCapacity)
  if (!Number.isInteger(capacity) || capacity < 1) errors.maxCapacity = 'Capacity must be a whole number of at least 1.'
  else if (capacity > 100_000) errors.maxCapacity = 'Capacity must be 100,000 or fewer.'
  else if (capacity < minCapacity)
    errors.maxCapacity = `Capacity can't be lower than the ${minCapacity} people already registered.`
  return errors
}

export function EventForm({ initial, submitLabel, submitting, error, onSubmit, onCancel }: Props) {
  const [fields, setFields] = useState<Fields>({
    title: initial?.title ?? '',
    description: initial?.description ?? '',
    date: initial ? toLocalInputValue(initial.date) : '',
    maxCapacity: initial ? String(initial.maxCapacity) : '20',
  })
  const [touched, setTouched] = useState(false)

  const clientErrors = validate(fields, initial?.registeredCount ?? 0)
  const serverErrors = error instanceof ApiError ? error.fieldErrors : {}
  const errors: FieldErrors = touched ? { ...serverErrors, ...clientErrors } : serverErrors
  const generalError = error && !Object.keys(serverErrors).length ? error.message : null

  const set = (key: keyof Fields) => (e: { target: { value: string } }) =>
    setFields((f) => ({ ...f, [key]: e.target.value }))

  function handleSubmit(e: FormEvent) {
    e.preventDefault()
    setTouched(true)
    if (Object.keys(clientErrors).length) return
    onSubmit({
      title: fields.title.trim(),
      description: fields.description.trim() || null,
      date: fromLocalInputValue(fields.date),
      maxCapacity: Number(fields.maxCapacity),
    })
  }

  return (
    <form className="form" onSubmit={handleSubmit} noValidate>
      {generalError && <div className="alert alert--error" role="alert">{generalError}</div>}

      <Field id="title" label="Title" error={errors.title}>
        <input id="title" value={fields.title} onChange={set('title')} maxLength={200} autoFocus
          placeholder="e.g. Quarterly Town Hall" aria-invalid={!!errors.title} />
      </Field>

      <Field id="description" label="Description" hint="Optional" error={errors.description}>
        <textarea id="description" rows={4} value={fields.description} onChange={set('description')}
          maxLength={4000} placeholder="What should attendees know?" aria-invalid={!!errors.description} />
      </Field>

      <div className="form__row">
        <Field id="date" label="Date & time" error={errors.date}>
          <input id="date" type="datetime-local" value={fields.date} onChange={set('date')}
            min={toLocalInputValue(new Date())} aria-invalid={!!errors.date} />
        </Field>

        <Field id="maxCapacity" label="Max capacity" error={errors.maxCapacity}>
          <input id="maxCapacity" type="number" inputMode="numeric" min={Math.max(1, initial?.registeredCount ?? 0)}
            max={100000} value={fields.maxCapacity} onChange={set('maxCapacity')} aria-invalid={!!errors.maxCapacity} />
        </Field>
      </div>

      <div className="form__actions">
        <button type="button" className="btn btn--secondary" onClick={onCancel} disabled={submitting}>
          Cancel
        </button>
        <button type="submit" className="btn btn--primary" disabled={submitting}>
          {submitting ? 'Saving…' : submitLabel}
        </button>
      </div>
    </form>
  )
}

function Field({ id, label, hint, error, children }: {
  id: string; label: string; hint?: string; error?: string; children: ReactNode
}) {
  return (
    <div className={`field${error ? ' field--invalid' : ''}`}>
      <label htmlFor={id}>
        {label} {hint && <span className="muted">({hint})</span>}
      </label>
      {children}
      {error && <p className="field__error">{error}</p>}
    </div>
  )
}
