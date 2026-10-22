using EventManagement.Api.Common;
using EventManagement.Api.Contracts;
using EventManagement.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace EventManagement.Api.Controllers;

[ApiController]
[Route("api/events/{eventId:guid}/registrations")]
[Produces("application/json")]
public sealed class RegistrationsController(IEventService eventService) : ControllerBase
{
    /// <summary>Lists the attendees registered for an event.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<RegistrationResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public ActionResult<IReadOnlyList<RegistrationResponse>> GetRegistrations(Guid eventId)
    {
        var result = eventService.GetRegistrations(eventId);
        return result.IsSuccess ? Ok(result.Value) : this.ToProblem(result.Error!);
    }

    /// <summary>
    /// Registers an attendee. Fails with 409 if the event is in the past, full,
    /// or the email address is already registered.
    /// </summary>
    [HttpPost]
    [ProducesResponseType<RegistrationResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public ActionResult<RegistrationResponse> Register(Guid eventId, RegistrationRequest request)
    {
        var result = eventService.Register(eventId, request);
        return result.IsSuccess
            ? Created($"/api/events/{eventId}/registrations/{result.Value.Id}", result.Value)
            : this.ToProblem(result.Error!);
    }

    /// <summary>Cancels a registration.</summary>
    [HttpDelete("{registrationId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public ActionResult Unregister(Guid eventId, Guid registrationId)
    {
        var result = eventService.Unregister(eventId, registrationId);
        return result.IsSuccess ? NoContent() : this.ToProblem(result.Error!);
    }
}
