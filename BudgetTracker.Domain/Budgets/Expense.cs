using BudgetTracker.Domain.Core;   // so we can inherit from Entity

namespace BudgetTracker.Domain.Budgets;

/// <summary>
///   One purchase in a budget: how much, what for, and when.
///   Only a Budget can create or remove one.
/// </summary>

// An Expense is an entity: it has a Guid Id, and two expenses
// are only equal if their Ids match.
// "sealed" = finished class, nothing inherits from it (same as Chris's ExperienceTracker).
public sealed class Expense : Entity<Guid>
{
    // internal constructor: only code inside the Domain project can call it.
    // The API can't write "new Expense(...)", so it has to go through budget.AddExpense(...), which keeps the budget's totals in sync.
    internal Expense(Guid id, Money amount, string description, DateOnly date)
        : base(id)
    {
        Amount = amount;
        Description = description;
        Date = date;
    }

    // public get: anyone can read these.
    // private set: only code inside this class can change them so outside code (including the API) can never edit an expense directly.

    // uses money to ensure amount is valid.
    public Money Amount { get; private set; }

    // what the money was spent on, e.g. "Coffee at Starbucks".
    public string Description { get; private set; }

    // date of purchase, time is not needed so DateOnly is sufficient.
    public DateOnly Date { get; private set; }
}