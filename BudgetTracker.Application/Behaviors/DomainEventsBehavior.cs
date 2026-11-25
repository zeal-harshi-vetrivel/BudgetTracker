using BudgetTracker.Application.Events;   // IDomainEventCollector, DomainEventNotification
using BudgetTracker.Domain.Core;          // IDomainEvent
using MediatR;                            // IPipelineBehavior, IPublisher

namespace BudgetTracker.Application.Behaviors;

/// <summary>
///   After a command finishes successfully, sends out the domain events it produced.
/// </summary>

// runs around every command and query (queries just never produce events).
// IPublisher = MediatR's door for notifications: it delivers to EVERY matching handler (0, 1 or many).
public sealed class DomainEventsBehavior<TRequest, TResponse>(
    IDomainEventCollector collector,
    IPublisher publisher)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // run the command handler first. If a rule is broken it throws here,
        // and we never reach the publishing below, so failed actions send no events.
        TResponse response = await next();

        // the handler finished and the repository already saved, so these events describe saved facts.
        foreach (IDomainEvent domainEvent in collector.TakeAll())
        {
            // build DomainEventNotification<ExpenseAddedDomainEvent> (or whatever the event's type is)
            // so MediatR can find the handlers for that specific event.
            Type notificationType = typeof(DomainEventNotification<>).MakeGenericType(domainEvent.GetType());
            object notification = Activator.CreateInstance(notificationType, domainEvent)!;

            await publisher.Publish(notification, cancellationToken);
        }

        return response;
    }
}
