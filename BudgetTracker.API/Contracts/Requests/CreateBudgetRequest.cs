namespace BudgetTracker.API.Contracts.Requests;

//  What the client sends to create a budget, with optional starting expenses.
public sealed class CreateBudgetRequest
{
    // the budget's name, e.g. "Groceries". required = the JSON must include it.
    public required string Name { get; init; }

    // the spending limit as a plain number, e.g. 400.00.
    // we use decimal here, not Money. Money is a Domain type and stays out of the HTTP contract.
    public required decimal Limit { get; init; }

    // expenses to add right away. Defaults to an empty list if the client leaves it out.
    public List<CreateExpenseRequest> Expenses { get; init; } = [];
}

//  One expense inside a create-budget request.
public sealed class CreateExpenseRequest
{
    // how much was spent, e.g. 52.75.
    public required decimal Amount { get; init; }

    // what it was for, e.g. "Costco run".
    public required string Description { get; init; }

    // when it happened. Optional: the controller will use today if it's missing.
    public DateOnly? Date { get; init; }
}