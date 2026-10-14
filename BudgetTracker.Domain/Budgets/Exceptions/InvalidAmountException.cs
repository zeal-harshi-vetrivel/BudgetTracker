using BudgetTracker.Domain.Core;   // so this file can see DomainException

namespace BudgetTracker.Domain.Budgets.Exceptions;

/// <summary>
///   Thrown when an amount of money isn't allowed, like a negative number.
/// </summary>

public class InvalidAmountException(string message) : DomainException(message);