using BudgetTracker.Application.Data.Queries;   // IBudgetQueries
using MediatR;                                  // IRequestHandler

namespace BudgetTracker.Application.Budgets.Queries;

/// <summary>
///   Answers GetBudgetByIdQuery. Reads only: no Domain objects, no Save.
/// </summary>

// compare with the command handlers: they use IBudgetRepository, call Domain methods and Save.
// this one only asks IBudgetQueries for data and passes it back.
public sealed class GetBudgetByIdQueryHandler(IBudgetQueries queries)
    : IRequestHandler<GetBudgetByIdQuery, BudgetDetails?>
{
    public Task<BudgetDetails?> Handle(GetBudgetByIdQuery query, CancellationToken cancellationToken) =>
        queries.GetBudgetById(query.BudgetId, cancellationToken);
}
