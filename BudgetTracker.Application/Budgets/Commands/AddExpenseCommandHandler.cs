using BudgetTracker.Application.Data.Repositories;   // IBudgetRepository
using BudgetTracker.Domain.Budgets;                  // Budget
using MediatR;                                       // IRequestHandler

namespace BudgetTracker.Application.Budgets.Commands;

/// <summary>
///   Does the work for AddExpenseCommand: loads the budget, adds the expense through the Domain, saves it.
/// </summary>
public sealed class AddExpenseCommandHandler(IBudgetRepository repository)
    : IRequestHandler<AddExpenseCommand, Budget?>
{
    public async Task<Budget?> Handle(AddExpenseCommand command, CancellationToken cancellationToken)
    {
        // rebuild the saved budget from the database.
        Budget? budget = await repository.Get(command.BudgetId, cancellationToken);

        // no budget with that Id. Return null and let the caller decide what that means (the API sends 404).
        if (budget is null)
            return null;

        // no date sent: use today.
        DateOnly date = command.Expense.Date ?? DateOnly.FromDateTime(DateTime.Today);

        // same Domain method as when creating, so the same rules apply. Throws if a rule is broken.
        budget.AddExpense(command.Expense.Amount, command.Expense.Description, date);

        // rules passed: save. The budget already exists, so Save takes the "update" path.
        await repository.Save(budget, cancellationToken);

        return budget;
    }
}
