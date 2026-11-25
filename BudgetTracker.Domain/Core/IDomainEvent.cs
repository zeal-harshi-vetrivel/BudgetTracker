namespace BudgetTracker.Domain.Core;

/// <summary>
///   Something that happened in the domain, named in the past tense (e.g. ExpenseAdded).
/// </summary>

// just a label. It lets the aggregate keep a list of "things that happened"
// without knowing anything about MediatR, HTTP, or the database.
public interface IDomainEvent;
