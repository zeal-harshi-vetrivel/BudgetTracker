using BudgetTracker.Domain.Budgets;   // Budget
using MediatR;                        // IRequest

namespace BudgetTracker.Application.Budgets.Commands;

/// <summary>
///   "Create a new budget, with these starting expenses." Just data, no logic.
/// </summary>

// IRequest<Budget> = when this command is handled, you get the new Budget back.
// it uses plain C# types only (no HTTP request classes), so a CLI or background job could send it too.
public sealed record CreateBudgetCommand(
    string Name,
    decimal Limit,
    IReadOnlyList<NewExpense> Expenses) : IRequest<Budget>;

/// <summary>
///   One expense to add. Used by CreateBudgetCommand and AddExpenseCommand.
/// </summary>

// Date is optional. The handler uses today if it's missing.
public sealed record NewExpense(decimal Amount, string Description, DateOnly? Date);
