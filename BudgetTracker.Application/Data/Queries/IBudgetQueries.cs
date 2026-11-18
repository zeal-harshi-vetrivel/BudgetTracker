using BudgetTracker.Application.Budgets.Queries;   // BudgetDetails

namespace BudgetTracker.Application.Data.Queries;

/// <summary>
///   Read-only lookups for budgets. Application says what it needs; the Data project decides how.
/// </summary>

// the read-side partner of IBudgetRepository.
// the repository loads and saves full Budget aggregates (for commands).
// this only reads data into read models (for queries), and has no Save.
public interface IBudgetQueries
{
    // one budget with its expenses, or null if no budget has that Id.
    Task<BudgetDetails?> GetBudgetById(Guid id, CancellationToken cancellationToken = default);
}
