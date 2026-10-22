import { useDeferredValue, useMemo, useState } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { useEvents } from '../api/hooks'
import type { EventStatus } from '../api/types'
import { EventCard } from '../components/EventCard'
import { EmptyState, ErrorState, Spinner } from '../components/Feedback'

const TABS: { value: EventStatus; label: string }[] = [
  { value: 'upcoming', label: 'Upcoming' },
  { value: 'past', label: 'Past' },
  { value: 'all', label: 'All' },
]

export function EventsPage() {
  // The selected tab lives in the URL so it survives refreshes and back navigation.
  const [params, setParams] = useSearchParams()
  const status = (TABS.find((t) => t.value === params.get('status'))?.value ?? 'upcoming') as EventStatus
  const [search, setSearch] = useState('')
  const deferredSearch = useDeferredValue(search)

  const { data, isPending, isFetching, error, refetch } = useEvents(status)

  const filtered = useMemo(() => {
    const q = deferredSearch.trim().toLowerCase()
    if (!q || !data) return data
    return data.filter((e) => e.title.toLowerCase().includes(q) || e.description?.toLowerCase().includes(q))
  }, [data, deferredSearch])

  return (
    <>
      <div className="page__header">
        <div>
          <h1>Events</h1>
          <p className="muted">Browse events and manage attendee registrations.</p>
        </div>
      </div>

      <div className="toolbar">
        <div className="tabs" role="tablist" aria-label="Filter events">
          {TABS.map((t) => (
            <button key={t.value} type="button" role="tab" aria-selected={status === t.value}
              className={`tab${status === t.value ? ' tab--active' : ''}`}
              onClick={() => setParams(t.value === 'upcoming' ? {} : { status: t.value })}>
              {t.label}
            </button>
          ))}
        </div>
        <input type="search" className="search" placeholder="Search events…" value={search}
          onChange={(e) => setSearch(e.target.value)} aria-label="Search events" />
      </div>

      {isPending ? (
        <Spinner label="Loading events…" />
      ) : error ? (
        <ErrorState error={error} onRetry={() => refetch()} />
      ) : !filtered?.length ? (
        <EmptyState title={search ? 'No matching events' : `No ${status === 'all' ? '' : status} events`}>
          {search ? (
            <p className="muted">Try a different search term.</p>
          ) : (
            status !== 'past' && <Link to="/events/new" className="btn btn--primary">Create an event</Link>
          )}
        </EmptyState>
      ) : (
        <div className={`event-grid${isFetching ? ' is-refreshing' : ''}`}>
          {filtered.map((e) => <EventCard key={e.id} event={e} />)}
        </div>
      )}
    </>
  )
}
