import { json, request } from './client'
import type { EventInput, EventItem, EventStatus, Registration, RegistrationInput } from './types'

export const eventsApi = {
  list: (status: EventStatus = 'all') =>
    request<EventItem[]>(`/api/events?status=${status}`),

  get: (id: string) =>
    request<EventItem>(`/api/events/${id}`),

  create: (input: EventInput) =>
    request<EventItem>('/api/events', { method: 'POST', body: json(input) }),

  update: (id: string, input: EventInput) =>
    request<EventItem>(`/api/events/${id}`, { method: 'PUT', body: json(input) }),

  remove: (id: string) =>
    request<void>(`/api/events/${id}`, { method: 'DELETE' }),

  registrations: (eventId: string) =>
    request<Registration[]>(`/api/events/${eventId}/registrations`),

  register: (eventId: string, input: RegistrationInput) =>
    request<Registration>(`/api/events/${eventId}/registrations`, { method: 'POST', body: json(input) }),

  unregister: (eventId: string, registrationId: string) =>
    request<void>(`/api/events/${eventId}/registrations/${registrationId}`, { method: 'DELETE' }),
}
