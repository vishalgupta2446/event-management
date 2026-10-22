// Mirrors the backend contracts in EventManagement.Api/Contracts.

export type EventStatus = 'all' | 'upcoming' | 'past'

export interface EventItem {
  id: string
  title: string
  description: string | null
  /** ISO-8601 timestamp with offset. */
  date: string
  maxCapacity: number
  registeredCount: number
  remainingCapacity: number
  isFull: boolean
  isPast: boolean
  createdAt: string
}

export interface EventInput {
  title: string
  description: string | null
  date: string
  maxCapacity: number
}

export interface Registration {
  id: string
  eventId: string
  name: string
  email: string
  registeredAt: string
}

export interface RegistrationInput {
  name: string
  email: string
}
