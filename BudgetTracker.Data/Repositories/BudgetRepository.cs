using BudgetTracker.Application.Data.Repositories;   // IBudgetRepository (the contract we're fulfilling)
using BudgetTracker.Application.Events;              // IDomainEventCollector
using BudgetTracker.Data.Entities;                   // BudgetEntity, ExpenseEntity
using BudgetTracker.Domain.Budgets;                  // Budget, Expense, Money
using Microsoft.EntityFrameworkCore;                 // Include, FirstOrDefaultAsync

namespace BudgetTracker.Data.Repositories;

/// <summary>
///   Saves and loads budgets with EF Core, translating between Domain objects and table rows.
/// </summary>

// ": IBudgetRepository" = this class promises to do everything the interface lists.
// (BudgetTrackerContext db) = EF Core's database connection, handed in at startup.
// (IDomainEventCollector events) = where we drop the budget's domain events for the unit of work.
internal sealed class BudgetRepository(BudgetTrackerContext db, IDomainEventCollector events) : IBudgetRepository
{
    // LOAD: rows -> Domain
    public async Task<Budget?> Get(Guid id, CancellationToken cancellationToken = default)
    {
        // find the budget row with this Id. Include = also load its expense rows.
        BudgetEntity? entity = await db.Budgets
            .Include(b => b.Expenses)
            .FirstOrDefaultAsync(b => b.BudgetId == id, cancellationToken);

        // no row with that Id: tell the caller "not found".
        if (entity is null)
            return null;

        // rebuild each Expense with its SAVED Id (internal constructor, allowed by InternalsVisibleTo).
        var expenses = entity.Expenses.Select(e =>
            new Expense(e.ExpenseId, Money.From(e.Amount), e.Description, e.Date));

        // rebuild the Budget with the constructor from Step 6a. It works out Spent from the expenses.
        return new Budget(entity.BudgetId, entity.Name, Money.From(entity.Limit), expenses);
    }

    // SAVE: Domain -> rows (insert if new, update if it already exists)
    // this only STAGES the changes. Nothing is written until the unit of work commits
    // (UnitOfWork.Commit -> IDataStore.SaveChanges), as Chris showed in the Nov 11 session.
    public async Task Save(Budget budget, CancellationToken cancellationToken = default)
    {
        // is this budget already staged in this request (Local = rows EF is already tracking)?
        // if not, is it in the database?
        BudgetEntity? entity = db.Budgets.Local.FirstOrDefault(b => b.BudgetId == budget.Id)
            ?? await db.Budgets
                .Include(b => b.Expenses)
                .FirstOrDefaultAsync(b => b.BudgetId == budget.Id, cancellationToken);

        // not there yet: make a new row and tell EF to INSERT it.
        if (entity is null)
        {
            entity = new BudgetEntity { BudgetId = budget.Id };
            db.Budgets.Add(entity);
        }

        // copy the budget's values onto the row (pull the decimal out of Money).
        entity.Name = budget.Name;
        entity.Limit = budget.Limit.Amount;

        // expenses can only be added in our Domain (never edited or removed),
        // so we only need to insert the ones that aren't saved yet.
        foreach (Expense expense in budget.Expenses)
        {
            // already saved? skip it.
            if (entity.Expenses.Any(e => e.ExpenseId == expense.Id))
                continue;

            // new expense: tell EF to INSERT it, linked to this budget by BudgetId.
            db.Expenses.Add(new ExpenseEntity
            {
                ExpenseId = expense.Id,
                BudgetId = budget.Id,
                Amount = expense.Amount.Amount,
                Description = expense.Description,
                Date = expense.Date
            });
        }

        // no SaveChangesAsync here: the unit of work saves everything once, at the end of the command.

        // move the budget's events into the request-wide collector.
        // the unit of work sends them out, then saves (Chris's Nov 18 flow).
        // PullDomainEvents is internal to Domain, and we can call it thanks to InternalsVisibleTo.
        events.Add(budget.PullDomainEvents());
    }
}