namespace BudgetTracker.Data.Entities;

//   One row in the Expenses table. Plain data only, no rules.

// internal: only the Data project can see this. The API and Application never touch table classes.
internal sealed class ExpenseEntity
{
    // primary key. Same value as the Domain Expense's Id.
    public Guid ExpenseId { get; set; }

    // foreign key: which budget this expense belongs to.
    public Guid BudgetId { get; set; }

    // plain decimal here. The Money rules already ran in the Domain before we got here.
    public decimal Amount { get; set; }

    // default to empty text so it's never null.
    public string Description { get; set; } = string.Empty;

    public DateOnly Date { get; set; }
}