const dateTimeFormat = new Intl.DateTimeFormat(undefined, {
  weekday: 'short',
  month: 'short',
  day: 'numeric',
  year: 'numeric',
  hour: 'numeric',
  minute: '2-digit',
})

const shortDateFormat = new Intl.DateTimeFormat(undefined, { month: 'short', day: 'numeric', year: 'numeric' })
const monthFormat = new Intl.DateTimeFormat(undefined, { month: 'short' })
const timeFormat = new Intl.DateTimeFormat(undefined, { hour: 'numeric', minute: '2-digit' })

export const formatDateTime = (iso: string) => dateTimeFormat.format(new Date(iso))
export const formatShortDate = (iso: string) => shortDateFormat.format(new Date(iso))
export const formatTime = (iso: string) => timeFormat.format(new Date(iso))

export function dateParts(iso: string) {
  const d = new Date(iso)
  return { month: monthFormat.format(d), day: d.getDate() }
}

const pad = (n: number) => String(n).padStart(2, '0')

/** ISO timestamp -> value for <input type="datetime-local"> in the user's time zone. */
export function toLocalInputValue(iso: string | Date): string {
  const d = new Date(iso)
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`
}

/** <input type="datetime-local"> value (local time) -> ISO timestamp in UTC. */
export const fromLocalInputValue = (value: string) => new Date(value).toISOString()

export function pluralize(count: number, singular: string, plural = `${singular}s`) {
  return `${count} ${count === 1 ? singular : plural}`
}
