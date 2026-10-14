namespace BudgetTracker.Domain.Core;

/// <summary>
///   Thrown when someone tries to break a business rule, like adding a negative expense.
/// </summary>
/// abstract ensures that we have to create specific exceptions for different business rules that inherit this one. That way, every error has clear business context/terminology. 
/// Example: NegativeExpenseException inherits DomainException and is thrown when someone tries to add a negative expense.
public abstract class DomainException(string message) : Exception(message);