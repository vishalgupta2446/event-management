using EventManagement.Api.Common;
using EventManagement.Api.Contracts;

namespace EventManagement.Api.Services;

public interface IEventService
{
    IReadOnlyList<EventResponse> GetEvents(EventStatus status = EventStatus.All);
    Result<EventResponse> GetEvent(Guid id);
    Result<EventResponse> CreateEvent(EventRequest request);
    Result<EventResponse> UpdateEvent(Guid id, EventRequest request);
    Result DeleteEvent(Guid id);

    Result<IReadOnlyList<RegistrationResponse>> GetRegistrations(Guid eventId);
    Result<RegistrationResponse> Register(Guid eventId, RegistrationRequest request);
    Result Unregister(Guid eventId, Guid registrationId);
}
