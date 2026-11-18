namespace BudgetTracker.Application.Budgets.Queries;

/// <summary>
///   Read model: a budget as data to show, not a Domain object. No rules, no methods that change anything.
/// </summary>

// records with plain types only. The Data layer fills these straight from the database,
// so reading never builds a Budget aggregate or runs domain rules.
public sealed record BudgetDetails(
    Guid Id,
    string Name,
    decimal Limit,
    decimal Spent,
    decimal Remaining,
    IReadOnlyList<ExpenseDetails> Expenses);

/// <summary>
///   One expense inside BudgetDetails.
/// </summary>
public sealed record ExpenseDetails(Guid Id, decimal Amount, string Description, DateOnly Date);
