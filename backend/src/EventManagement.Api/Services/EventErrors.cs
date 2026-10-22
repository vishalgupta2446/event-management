using EventManagement.Api.Common;

namespace EventManagement.Api.Services;

/// <summary>Catalogue of business-rule errors returned by <see cref="EventService"/>.</summary>
public static class EventErrors
{
    public static Error EventNotFound(Guid id) =>
        Error.NotFound("event_not_found", $"Event '{id}' was not found.");

    public static Error RegistrationNotFound(Guid id) =>
        Error.NotFound("registration_not_found", $"Registration '{id}' was not found for this event.");

    public static readonly Error DateInPast =
        Error.Validation("event_date_in_past", "Event date must be in the future.");

    public static readonly Error TitleRequired =
        Error.Validation("title_required", "Title is required.");

    public static readonly Error PastEventLocked =
        Error.Conflict("event_in_past", "Past events cannot be modified.");

    public static Error CapacityBelowRegistrations(int registered) =>
        Error.Conflict("capacity_below_registrations",
            $"Capacity cannot be lower than the current number of registrations ({registered}).");

    public static readonly Error RegistrationClosed =
        Error.Conflict("event_in_past", "Cannot register for an event that has already taken place.");

    public static readonly Error UnregistrationClosed =
        Error.Conflict("event_in_past", "Cannot unregister from an event that has already taken place.");

    public static readonly Error EventFull =
        Error.Conflict("event_full", "This event has reached its maximum capacity.");

    public static readonly Error AlreadyRegistered =
        Error.Conflict("already_registered", "This email address is already registered for this event.");
}
