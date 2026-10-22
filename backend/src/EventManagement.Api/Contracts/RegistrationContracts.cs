using System.ComponentModel.DataAnnotations;
using EventManagement.Api.Models;

namespace EventManagement.Api.Contracts;

public sealed record RegistrationRequest
{
    [Required, StringLength(200, MinimumLength = 1)]
    public string Name { get; init; } = string.Empty;

    [Required, EmailAddress, StringLength(320)]
    public string Email { get; init; } = string.Empty;
}

public sealed record RegistrationResponse(
    Guid Id,
    Guid EventId,
    string Name,
    string Email,
    DateTimeOffset RegisteredAt)
{
    public static RegistrationResponse From(Registration r) =>
        new(r.Id, r.EventId, r.Name, r.Email, r.RegisteredAt);
}
