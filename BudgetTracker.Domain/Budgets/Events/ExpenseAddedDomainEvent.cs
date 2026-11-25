using BudgetTracker.Domain.Core;   // IDomainEvent

namespace BudgetTracker.Domain.Budgets.Events;

/// <summary>
///   An expense was added to a budget. Raised only after every rule in AddExpense passed.
/// </summary>

// past tense: it describes a fact that is already true, not a request.
// plain data only (no HTTP, no EF Core), so anything can react to it.
public sealed record ExpenseAddedDomainEvent(
    Guid BudgetId,
    string BudgetName,
    Guid ExpenseId,
    Money Amount,
    Money Remaining) : IDomainEvent;
