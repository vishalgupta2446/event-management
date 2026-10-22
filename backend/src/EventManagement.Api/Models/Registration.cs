namespace EventManagement.Api.Models;

/// <summary>
/// An attendee's registration for an event. Authentication is out of scope, so an
/// attendee is identified by email address (compared case-insensitively).
/// </summary>
public sealed record Registration
{
    public required Guid Id { get; init; }
    public required Guid EventId { get; init; }
    public required string Name { get; init; }
    public required string Email { get; init; }
    public required DateTimeOffset RegisteredAt { get; init; }
}
