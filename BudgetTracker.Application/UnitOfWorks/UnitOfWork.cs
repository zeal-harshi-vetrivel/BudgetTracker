using BudgetTracker.Application.Data;     // IDataStore
using BudgetTracker.Application.Events;   // IDomainEventCollector, DomainEventNotification
using BudgetTracker.Domain.Core;          // IDomainEvent
using MediatR;                            // IPublisher

namespace BudgetTracker.Application.UnitOfWorks;

/// <summary>
///   Finishes one piece of work: runs the reactions to its domain events, then saves everything together.
/// </summary>
public interface IUnitOfWork
{
    Task Commit(CancellationToken cancellationToken = default);
}

// same order as Chris's UnitOfWork.Commit (Nov 18 session):
//   1. dispatch the domain events the repository collected
//   2. THEN save all staged changes in one SaveChanges
// so the command's changes and its side effects succeed or fail together.
internal sealed class UnitOfWork(
    IDomainEventCollector collector,
    IPublisher publisher,
    IDataStore dataStore) : IUnitOfWork
{
    public async Task Commit(CancellationToken cancellationToken = default)
    {
        // keep going until there are no events left, because a reaction could
        // change and save another aggregate, which could raise more events.
        IReadOnlyList<IDomainEvent> events;
        while ((events = collector.TakeAll()).Count > 0)
        {
            foreach (IDomainEvent domainEvent in events)
            {
                // wrap the plain domain event so MediatR can deliver it,
                // e.g. DomainEventNotification<ExpenseAddedDomainEvent>.
                Type notificationType = typeof(DomainEventNotification<>).MakeGenericType(domainEvent.GetType());
                object notification = Activator.CreateInstance(notificationType, domainEvent)!;

                await publisher.Publish(notification, cancellationToken);
            }
        }

        // every reaction finished: write all staged changes to the database at once.
        await dataStore.SaveChanges(cancellationToken);
    }
}
