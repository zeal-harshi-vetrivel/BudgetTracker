using BudgetTracker.Domain.Core;   // so this file can see DomainException

namespace BudgetTracker.Domain.Budgets.Exceptions;

/// <summary>
///   Thrown when an expense would push spending past the budget's limit.
/// </summary>

public class BudgetExceededException(string message) : DomainException(message);