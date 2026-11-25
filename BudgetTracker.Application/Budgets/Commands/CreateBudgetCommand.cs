using BudgetTracker.Domain.Budgets;   // Budget
using BudgetTracker.Application.Commands;   // CommandBase

namespace BudgetTracker.Application.Budgets.Commands;

/// <summary>
///   "Create a new budget, with these starting expenses." Just data, no logic.
/// </summary>

// CommandBase<Budget> = it's a command (so it runs in a unit of work), and when it's handled you get the new Budget back.
// it uses plain C# types only (no HTTP request classes), so a CLI or background job could send it too.
public sealed record CreateBudgetCommand(
    string Name,
    decimal Limit,
    IReadOnlyList<NewExpense> Expenses) : CommandBase<Budget>;

/// <summary>
///   One expense to add. Used by CreateBudgetCommand and AddExpenseCommand.
/// </summary>

// Date is optional. The handler uses today if it's missing.
public sealed record NewExpense(decimal Amount, string Description, DateOnly? Date);
