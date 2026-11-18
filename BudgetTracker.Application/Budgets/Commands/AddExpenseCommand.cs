using BudgetTracker.Domain.Budgets;   // Budget
using MediatR;                        // IRequest

namespace BudgetTracker.Application.Budgets.Commands;

/// <summary>
///   "Add this expense to the saved budget with this Id."
/// </summary>

// IRequest<Budget?> = you get the updated Budget back, or null if no budget has that Id.
public sealed record AddExpenseCommand(Guid BudgetId, NewExpense Expense) : IRequest<Budget?>;
