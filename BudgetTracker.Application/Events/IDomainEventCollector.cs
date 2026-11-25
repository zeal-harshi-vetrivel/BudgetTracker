using BudgetTracker.Domain.Core;   // IDomainEvent

namespace BudgetTracker.Application.Events;

/// <summary>
///   Holds domain events from aggregates that were saved during this request, until they're sent out.
/// </summary>

// the repository puts events in (after saving), DomainEventsBehavior takes them out (after the command).
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
