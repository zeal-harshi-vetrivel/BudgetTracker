using BudgetTracker.Domain.Core;   // IDomainEvent
using MediatR;                     // INotification

namespace BudgetTracker.Application.Events;

/// <summary>
///   Wraps a domain event so MediatR can deliver it to handlers.
/// </summary>

// MediatR only delivers things that implement INotification.
// the Domain project doesn't reference MediatR (and shouldn't), so we wrap the event here instead.
// a handler for ExpenseAddedDomainEvent listens for DomainEventNotification<ExpenseAddedDomainEvent>.
public sealed record DomainEventNotification<TEvent>(TEvent DomainEvent) : INotification
    where TEvent : IDomainEvent;
