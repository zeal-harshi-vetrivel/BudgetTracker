using BudgetTracker.Domain.Core;   // IDomainEvent

namespace BudgetTracker.Application.Events;

/// <summary>
///   Holds domain events from aggregates saved (staged) during this request, until they're sent out.
/// </summary>

// the repository puts events in (when it stages a save), the unit of work takes them out (at commit).
// one collector per HTTP request (registered as Scoped), so requests never mix events.
public interface IDomainEventCollector
{
    void Add(IEnumerable<IDomainEvent> domainEvents);

    // returns everything collected so far and empties the collector.
    IReadOnlyList<IDomainEvent> TakeAll();
}

/// <summary>
///   The simple in-memory version: just a list.
/// </summary>
internal sealed class DomainEventCollector : IDomainEventCollector
{
    private readonly List<IDomainEvent> _events = [];

    public void Add(IEnumerable<IDomainEvent> domainEvents) => _events.AddRange(domainEvents);

    public IReadOnlyList<IDomainEvent> TakeAll()
    {
        List<IDomainEvent> events = [.. _events];
        _events.Clear();
        return events;
    }
}
