namespace EventManagement.Api.Models;

/// <summary>
/// An event that attendees can register for. Registrations are owned by the event
/// so capacity and duplicate checks can be enforced against a single aggregate.
/// </summary>
public sealed class Event
{
    private readonly List<Registration> _registrations = [];

    public required Guid Id { get; init; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    public required DateTimeOffset Date { get; set; }
    public required int MaxCapacity { get; set; }
    public required DateTimeOffset CreatedAt { get; init; }

    public IReadOnlyList<Registration> Registrations => _registrations;
    public int RegisteredCount => _registrations.Count;
    public int RemainingCapacity => Math.Max(0, MaxCapacity - _registrations.Count);
    public bool IsFull => _registrations.Count >= MaxCapacity;

    public bool IsPast(DateTimeOffset now) => Date <= now;

    public bool IsRegistered(string email) =>
        _registrations.Any(r => string.Equals(r.Email, email, StringComparison.OrdinalIgnoreCase));

    public Registration? FindRegistration(Guid registrationId) =>
        _registrations.FirstOrDefault(r => r.Id == registrationId);

    internal void AddRegistration(Registration registration) => _registrations.Add(registration);

    internal bool RemoveRegistration(Registration registration) => _registrations.Remove(registration);
}
