using BudgetTracker.Domain.Budgets;   // Budget
using BudgetTracker.Application.Commands;   // CommandBase

namespace BudgetTracker.Application.Budgets.Commands;

/// <summary>
///   "Add this expense to the saved budget with this Id."
/// </summary>

// CommandBase<Budget?> = it's a command (runs in a unit of work); you get the updated Budget back, or null if no budget has that Id.
public sealed record AddExpenseCommand(Guid BudgetId, NewExpense Expense) : CommandBase<Budget?>;
