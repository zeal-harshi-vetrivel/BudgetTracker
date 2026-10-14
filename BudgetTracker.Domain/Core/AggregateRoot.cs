namespace BudgetTracker.Domain.Core;

/// <summary>
///   The main object that owns other objects. All changes go through it.
///   Example: a Budget owns its Expenses.
/// </summary>
/// Inherits from entity and passes id along
public abstract class AggregateRoot<TId>(TId id) : Entity<TId>(id)
    where TId : notnull;