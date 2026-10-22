import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { ApiError } from './client'
import { eventsApi } from './events'
import type { EventInput, EventItem, EventStatus, RegistrationInput } from './types'

/**
 * Centralised query keys. Everything lives under ['events'] so a single
 * invalidation can refresh every list after a write.
 */
export const eventKeys = {
  all: ['events'] as const,
  lists: () => [...eventKeys.all, 'list'] as const,
  list: (status: EventStatus) => [...eventKeys.lists(), status] as const,
  detail: (id: string) => [...eventKeys.all, 'detail', id] as const,
  registrations: (id: string) => [...eventKeys.all, 'registrations', id] as const,
}

export function useEvents(status: EventStatus) {
  return useQuery({
    queryKey: eventKeys.list(status),
    queryFn: () => eventsApi.list(status),
    // Keep showing the previous tab's data while the next one loads.
    placeholderData: (previous) => previous,
  })
}

export function useEvent(id: string) {
  const queryClient = useQueryClient()
  return useQuery({
    queryKey: eventKeys.detail(id),
    queryFn: () => eventsApi.get(id),
    // Show the copy from any cached list while the fresh detail loads, so
    // navigating from the list renders instantly.
    placeholderData: () =>
      queryClient
        .getQueriesData<EventItem[]>({ queryKey: eventKeys.lists() })
        .flatMap(([, events]) => events ?? [])
        .find((e) => e.id === id),
    retry: (count, error) => !(error instanceof ApiError && error.status === 404) && count < 2,
  })
}

export function useRegistrations(eventId: string) {
  return useQuery({
    queryKey: eventKeys.registrations(eventId),
    queryFn: () => eventsApi.registrations(eventId),
  })
}

export function useCreateEvent() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (input: EventInput) => eventsApi.create(input),
    onSuccess: (created) => {
      queryClient.setQueryData(eventKeys.detail(created.id), created)
      return queryClient.invalidateQueries({ queryKey: eventKeys.lists() })
    },
  })
}

export function useUpdateEvent(id: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (input: EventInput) => eventsApi.update(id, input),
    onSuccess: (updated) => {
      queryClient.setQueryData(eventKeys.detail(id), updated)
      return queryClient.invalidateQueries({ queryKey: eventKeys.lists() })
    },
  })
}

export function useDeleteEvent() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => eventsApi.remove(id),
    onSuccess: (_, id) => {
      queryClient.removeQueries({ queryKey: eventKeys.detail(id) })
      queryClient.removeQueries({ queryKey: eventKeys.registrations(id) })
      return queryClient.invalidateQueries({ queryKey: eventKeys.lists() })
    },
  })
}

/** Refreshes the event's counts and every list after its registrations change. */
function invalidateEventCounts(queryClient: ReturnType<typeof useQueryClient>, eventId: string) {
  return Promise.all([
    queryClient.invalidateQueries({ queryKey: eventKeys.detail(eventId) }),
    queryClient.invalidateQueries({ queryKey: eventKeys.registrations(eventId) }),
    queryClient.invalidateQueries({ queryKey: eventKeys.lists() }),
  ])
}

export function useRegister(eventId: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (input: RegistrationInput) => eventsApi.register(eventId, input),
    // A 409 (full / past) means our cached counts are stale, so refresh on error too.
    onSettled: () => invalidateEventCounts(queryClient, eventId),
  })
}

export function useUnregister(eventId: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (registrationId: string) => eventsApi.unregister(eventId, registrationId),
    onSettled: () => invalidateEventCounts(queryClient, eventId),
  })
}
