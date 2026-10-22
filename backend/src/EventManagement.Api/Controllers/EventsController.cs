using EventManagement.Api.Common;
using EventManagement.Api.Contracts;
using EventManagement.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace EventManagement.Api.Controllers;

[ApiController]
[Route("api/events")]
[Produces("application/json")]
public sealed class EventsController(IEventService eventService) : ControllerBase
{
    /// <summary>Lists events, optionally filtered to upcoming or past ones.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<EventResponse>>(StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<EventResponse>> GetEvents([FromQuery] EventStatus status = EventStatus.All) =>
        Ok(eventService.GetEvents(status));

    /// <summary>Gets a single event.</summary>
    [HttpGet("{id:guid}", Name = nameof(GetEvent))]
    [ProducesResponseType<EventResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public ActionResult<EventResponse> GetEvent(Guid id)
    {
        var result = eventService.GetEvent(id);
        return result.IsSuccess ? Ok(result.Value) : this.ToProblem(result.Error!);
    }

    /// <summary>Creates an event. The date must be in the future.</summary>
    [HttpPost]
    [ProducesResponseType<EventResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public ActionResult<EventResponse> CreateEvent(EventRequest request)
    {
        var result = eventService.CreateEvent(request);
        return result.IsSuccess
            ? CreatedAtRoute(nameof(GetEvent), new { id = result.Value.Id }, result.Value)
            : this.ToProblem(result.Error!);
    }

    /// <summary>Replaces an event's details. Past events cannot be edited.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType<EventResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public ActionResult<EventResponse> UpdateEvent(Guid id, EventRequest request)
    {
        var result = eventService.UpdateEvent(id, request);
        return result.IsSuccess ? Ok(result.Value) : this.ToProblem(result.Error!);
    }

    /// <summary>Deletes an event and all of its registrations.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public ActionResult DeleteEvent(Guid id)
    {
        var result = eventService.DeleteEvent(id);
        return result.IsSuccess ? NoContent() : this.ToProblem(result.Error!);
    }
}
