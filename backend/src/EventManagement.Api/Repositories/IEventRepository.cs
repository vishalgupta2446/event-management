using EventManagement.Api.Models;

namespace EventManagement.Api.Repositories;

/// <summary>
/// Storage abstraction for events. The only implementation is in-memory, but the
/// service depends on this interface so a persistent store could be swapped in.
/// </summary>
public interface IEventRepository
{
    IReadOnlyCollection<Event> GetAll();
    Event? GetById(Guid id);
    void Add(Event evt);
    bool Remove(Guid id);
}
