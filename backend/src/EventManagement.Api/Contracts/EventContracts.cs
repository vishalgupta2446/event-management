using System.ComponentModel.DataAnnotations;
using EventManagement.Api.Models;

namespace EventManagement.Api.Contracts;

/// <summary>Payload for creating or fully updating (PUT) an event.</summary>
public sealed record EventRequest
{
    [Required, StringLength(200, MinimumLength = 1)]
    public string Title { get; init; } = string.Empty;

    [StringLength(4000)]
    public string? Description { get; init; }

    [Required]
    public DateTimeOffset? Date { get; init; }

    [Range(1, 100_000)]
    public int MaxCapacity { get; init; }
}

public sealed record EventResponse(
    Guid Id,
    string Title,
    string? Description,
    DateTimeOffset Date,
    int MaxCapacity,
    int RegisteredCount,
    int RemainingCapacity,
    bool IsFull,
    bool IsPast,
    DateTimeOffset CreatedAt)
{
    public static EventResponse From(Event e, DateTimeOffset now) => new(
        e.Id,
        e.Title,
        e.Description,
        e.Date,
        e.MaxCapacity,
        e.RegisteredCount,
        e.RemainingCapacity,
        e.IsFull,
        e.IsPast(now),
        e.CreatedAt);
}

/// <summary>Filter for listing events.</summary>
public enum EventStatus
{
    All,
    Upcoming,
    Past,
}
