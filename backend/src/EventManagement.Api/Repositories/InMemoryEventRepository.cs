using System.Collections.Concurrent;
using EventManagement.Api.Models;

namespace EventManagement.Api.Repositories;

/// <summary>
/// Thread-safe in-memory event store keyed by id (O(1) lookups).
/// Data is lost when the process stops.
/// </summary>
public sealed class InMemoryEventRepository : IEventRepository
{
    private readonly ConcurrentDictionary<Guid, Event> _events = new();

    public IReadOnlyCollection<Event> GetAll() => _events.Values.ToArray();

    public Event? GetById(Guid id) => _events.GetValueOrDefault(id);

    public void Add(Event evt)
    {
        if (!_events.TryAdd(evt.Id, evt))
        {
            throw new InvalidOperationException($"An event with id '{evt.Id}' already exists.");
        }
    }

    public bool Remove(Guid id) => _events.TryRemove(id, out _);
}
