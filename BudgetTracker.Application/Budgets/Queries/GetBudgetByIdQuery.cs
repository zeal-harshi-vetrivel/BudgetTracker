using MediatR;   // IRequest

namespace BudgetTracker.Application.Budgets.Queries;

/// <summary>
///   "Show me the budget with this Id." A question, so it never changes anything.
/// </summary>

// IRequest<BudgetDetails?> = you get the read model back, or null if no budget has that Id.
public sealed record GetBudgetByIdQuery(Guid BudgetId) : IRequest<BudgetDetails?>;
