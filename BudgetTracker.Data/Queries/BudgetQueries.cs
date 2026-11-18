using BudgetTracker.Application.Budgets.Queries;   // BudgetDetails, ExpenseDetails
using BudgetTracker.Application.Data.Queries;      // IBudgetQueries (the contract we're fulfilling)
using Microsoft.EntityFrameworkCore;               // AsNoTracking, SingleOrDefaultAsync

namespace BudgetTracker.Data.Queries;

/// <summary>
///   Reads budgets straight from the database into read models. Never builds Domain objects, never saves.
/// </summary>
internal sealed class BudgetQueries(BudgetTrackerContext db) : IBudgetQueries
{
    public async Task<BudgetDetails?> GetBudgetById(Guid id, CancellationToken cancellationToken = default)
    {
        var budget = await db.Budgets
            // AsNoTracking = "I'm only reading". EF skips change tracking, which is faster
            // and means nothing loaded here could be saved by accident.
            .AsNoTracking()
            .Where(b => b.BudgetId == id)
            // Select = projection: EF only asks the database for these columns,
            // and puts the expense rows straight into ExpenseDetails.
            .Select(b => new
            {
                b.BudgetId,
                b.Name,
                b.Limit,
                Expenses = b.Expenses
                    .OrderBy(e => e.Date)
                    .Select(e => new ExpenseDetails(e.ExpenseId, e.Amount, e.Description, e.Date))
                    .ToList()
            })
            .SingleOrDefaultAsync(cancellationToken);

        // no row with that Id.
        if (budget is null)
            return null;

        // SQLite stores decimals as text and can't add them up reliably,
        // so we add the amounts here in C# instead of in the database.
        decimal spent = budget.Expenses.Sum(e => e.Amount);

        return new BudgetDetails(
            budget.BudgetId,
            budget.Name,
            budget.Limit,
            spent,
            budget.Limit - spent,
            budget.Expenses);
    }
}
