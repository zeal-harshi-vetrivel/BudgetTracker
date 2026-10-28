using BudgetTracker.Domain.Budgets;   // so we can read Budget and Expense in From()

namespace BudgetTracker.API.Contracts.Responses;

//   What the client gets back for a budget.
public sealed class BudgetResponse
{
    public required Guid Id { get; init; }

    public required string Name { get; init; }

    // plain decimals, not Money, so the JSON shows 400.00 instead of { "amount": 400.00 }.
    public required decimal Limit { get; init; }

    public required decimal Spent { get; init; }

    public required decimal Remaining { get; init; }

    public required List<ExpenseResponse> Expenses { get; init; }

    // turns a Domain Budget into this response.
    // this is the only place that knows how the two shapes line up.
    public static BudgetResponse From(Budget budget) => new()
    {
        Id = budget.Id,
        Name = budget.Name,
        Limit = budget.Limit.Amount,       // pull the decimal out of the Money
        Spent = budget.Spent.Amount,
        Remaining = budget.Remaining.Amount,

        // turn each Expense into an ExpenseResponse, then collect them into a list
        Expenses = budget.Expenses.Select(ExpenseResponse.From).ToList()
    };
}

//  One expense in a budget response.
public sealed class ExpenseResponse
{
    public required Guid Id { get; init; }

    public required decimal Amount { get; init; }

    public required string Description { get; init; }

    public required DateOnly Date { get; init; }

    // turns a Domain Expense into this response.
    public static ExpenseResponse From(Expense expense) => new()
    {
        Id = expense.Id,
        Amount = expense.Amount.Amount,   // Expense.Amount is Money, and Money.Amount is the decimal
        Description = expense.Description,
        Date = expense.Date
    };
}