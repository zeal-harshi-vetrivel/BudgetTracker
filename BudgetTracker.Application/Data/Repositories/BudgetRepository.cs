using BudgetTracker.Domain.Budgets;   // Budget

namespace BudgetTracker.Application.Data.Repositories;


//  Saves and loads budgets. Application says what it needs; the Data project decides how.

public interface IBudgetRepository
{
    // load one budget by its Id, with all its expenses.
    // returns null if no budget has that Id (the "?" means it can be null).
    Task<Budget?> Get(Guid id, CancellationToken cancellationToken = default);

    // save a budget: insert it if it's new, update it if it already exists.
    Task Save(Budget budget, CancellationToken cancellationToken = default);
}