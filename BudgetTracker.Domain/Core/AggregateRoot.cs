namespace BudgetTracker.Domain.Core;

/// <summary>
///   The main object that owns other objects. All changes go through it.
///   Example: a Budget owns its Expenses.
///   It also keeps a list of domain events that happened but haven't been sent out yet.
/// </summary>
/// Inherits from entity and passes id along
public abstract class AggregateRoot<TId>(TId id) : Entity<TId>(id)
    where TId : notnull
{
    // events waiting to be sent out. Private, so only the aggregate can add to it.
    private readonly List<IDomainEvent> _domainEvents = [];

    // called by the aggregate's own methods (e.g. Budget.AddExpense) once something has really happened.
    // protected = only classes that inherit from AggregateRoot can call it.
    protected void AddDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    // hands back the waiting events and clears the list, so each event is sent out only once.
    // internal = only the Domain project and (through InternalsVisibleTo) the Data project can call it.
    // same idea as PullEvents in Chris's AggregateRoot.
    internal IReadOnlyList<IDomainEvent> PullDomainEvents()
    {
        List<IDomainEvent> events = [.. _domainEvents];
        _domainEvents.Clear();
        return events;
    }
}
