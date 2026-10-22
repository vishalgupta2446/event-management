using EventManagement.Api.Common;
using EventManagement.Api.Contracts;
using EventManagement.Api.Models;
using EventManagement.Api.Repositories;

namespace EventManagement.Api.Services;

/// <summary>
/// Owns all event and registration business rules:
/// <list type="bullet">
///   <item>Cannot register for past events.</item>
///   <item>Cannot exceed event capacity.</item>
///   <item>Cannot register the same attendee (email) twice for the same event.</item>
/// </list>
/// </summary>
/// <remarks>
/// Check-then-act sequences (e.g. "is there a free seat? then add") must be atomic, or two
/// concurrent requests could both take the last seat. A single lock keeps this simple and
/// obviously correct; for an in-memory store the critical sections are tiny, so contention
/// is negligible. Responses are mapped inside the lock so callers never observe a
/// half-updated event.
/// </remarks>
public sealed class EventService(IEventRepository repository, TimeProvider timeProvider) : IEventService
{
    private readonly Lock _gate = new();

    private DateTimeOffset Now => timeProvider.GetUtcNow();

    public IReadOnlyList<EventResponse> GetEvents(EventStatus status = EventStatus.All)
    {
        var now = Now;
        lock (_gate)
        {
            IEnumerable<Event> events = repository.GetAll();

            events = status switch
            {
                EventStatus.Upcoming => events.Where(e => !e.IsPast(now)),
                EventStatus.Past => events.Where(e => e.IsPast(now)),
                _ => events,
            };

            // Upcoming: soonest first. Past: most recent first.
            events = status == EventStatus.Past
                ? events.OrderByDescending(e => e.Date)
                : events.OrderBy(e => e.Date);

            return events.Select(e => EventResponse.From(e, now)).ToList();
        }
    }

    public Result<EventResponse> GetEvent(Guid id)
    {
        lock (_gate)
        {
            var evt = repository.GetById(id);
            return evt is null ? EventErrors.EventNotFound(id) : EventResponse.From(evt, Now);
        }
    }

    public Result<EventResponse> CreateEvent(EventRequest request)
    {
        var now = Now;
        var title = request.Title.Trim();

        if (title.Length == 0) return EventErrors.TitleRequired;
        if (request.Date is not { } date || date <= now) return EventErrors.DateInPast;

        var evt = new Event
        {
            Id = Guid.NewGuid(),
            Title = title,
            Description = NormalizeOptional(request.Description),
            Date = date,
            MaxCapacity = request.MaxCapacity,
            CreatedAt = now,
        };

        lock (_gate)
        {
            repository.Add(evt);
            return EventResponse.From(evt, now);
        }
    }

    public Result<EventResponse> UpdateEvent(Guid id, EventRequest request)
    {
        var now = Now;
        var title = request.Title.Trim();

        if (title.Length == 0) return EventErrors.TitleRequired;

        lock (_gate)
        {
            var evt = repository.GetById(id);
            if (evt is null) return EventErrors.EventNotFound(id);
            if (evt.IsPast(now)) return EventErrors.PastEventLocked;
            if (request.Date is not { } date || date <= now) return EventErrors.DateInPast;
            if (request.MaxCapacity < evt.RegisteredCount)
                return EventErrors.CapacityBelowRegistrations(evt.RegisteredCount);

            evt.Title = title;
            evt.Description = NormalizeOptional(request.Description);
            evt.Date = date;
            evt.MaxCapacity = request.MaxCapacity;

            return EventResponse.From(evt, now);
        }
    }

    public Result DeleteEvent(Guid id)
    {
        lock (_gate)
        {
            return repository.Remove(id) ? Result.Success() : EventErrors.EventNotFound(id);
        }
    }

    public Result<IReadOnlyList<RegistrationResponse>> GetRegistrations(Guid eventId)
    {
        lock (_gate)
        {
            var evt = repository.GetById(eventId);
            if (evt is null) return EventErrors.EventNotFound(eventId);

            return evt.Registrations
                .OrderBy(r => r.RegisteredAt)
                .Select(RegistrationResponse.From)
                .ToList();
        }
    }

    public Result<RegistrationResponse> Register(Guid eventId, RegistrationRequest request)
    {
        var now = Now;
        var name = request.Name.Trim();
        var email = request.Email.Trim();

        lock (_gate)
        {
            var evt = repository.GetById(eventId);
            if (evt is null) return EventErrors.EventNotFound(eventId);
            if (evt.IsPast(now)) return EventErrors.RegistrationClosed;
            if (evt.IsRegistered(email)) return EventErrors.AlreadyRegistered;
            if (evt.IsFull) return EventErrors.EventFull;

            var registration = new Registration
            {
                Id = Guid.NewGuid(),
                EventId = evt.Id,
                Name = name,
                Email = email,
                RegisteredAt = now,
            };
            evt.AddRegistration(registration);

            return RegistrationResponse.From(registration);
        }
    }

    public Result Unregister(Guid eventId, Guid registrationId)
    {
        lock (_gate)
        {
            var evt = repository.GetById(eventId);
            if (evt is null) return EventErrors.EventNotFound(eventId);

            var registration = evt.FindRegistration(registrationId);
            if (registration is null) return EventErrors.RegistrationNotFound(registrationId);
            if (evt.IsPast(Now)) return EventErrors.UnregistrationClosed;

            evt.RemoveRegistration(registration);
            return Result.Success();
        }
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
