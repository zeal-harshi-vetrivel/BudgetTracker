namespace BudgetTracker.Data.Entities;

//  One row in the Budgets table, plus its expense rows. Plain data only, no rules.

internal sealed class BudgetEntity
{
    // primary key. Same value as the Domain Budget's Id.
    public Guid BudgetId { get; set; }

    public string Name { get; set; } = string.Empty;

    // the spending limit as a plain decimal.
    public decimal Limit { get; set; }

    // no Spent column: it's the sum of the expenses, so we calculate it when loading.

    // all expense rows that belong to this budget (matched by ExpenseEntity.BudgetId).
    // starts as an empty list so it's never null.
    public List<ExpenseEntity> Expenses { get; set; } = [];
}