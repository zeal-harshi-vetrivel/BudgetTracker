using BudgetTracker.Application.Events;           // DomainEventNotification
using BudgetTracker.Domain.Budgets.Events;        // ExpenseAddedDomainEvent
using MediatR;                                    // INotificationHandler
using Microsoft.Extensions.Logging;               // ILogger

namespace BudgetTracker.Application.Budgets.EventHandlers;

/// <summary>
///   Reacts to ExpenseAdded by writing a log line. The Budget doesn't know this class exists.
/// </summary>

// more reactions (send an alert at 90% of the limit, update a report...) would be more handlers like this one.
// none of them would need a change to Budget.
public sealed class LogExpenseAddedHandler(ILogger<LogExpenseAddedHandler> logger)
    : INotificationHandler<DomainEventNotification<ExpenseAddedDomainEvent>>
{
    public Task Handle(
        DomainEventNotification<ExpenseAddedDomainEvent> notification,
        CancellationToken cancellationToken)
    {
        ExpenseAddedDomainEvent e = notification.DomainEvent;

        // structured logging: each {Name} becomes a separate field, not just text.
        logger.LogInformation(
            "ExpenseAdded: {Amount} added to '{BudgetName}' ({BudgetId}). {Remaining} left.",
            e.Amount, e.BudgetName, e.BudgetId, e.Remaining);

        return Task.CompletedTask;
    }
}
