using BudgetTracker.Application.Data.Repositories;   // IBudgetRepository
using BudgetTracker.Domain.Budgets;                  // Budget
using MediatR;                                       // IRequestHandler

namespace BudgetTracker.Application.Budgets.Commands;

/// <summary>
///   Does the work for CreateBudgetCommand: builds the budget through the Domain and saves it.
/// </summary>

// IRequestHandler<CreateBudgetCommand, Budget> = "I handle CreateBudgetCommand and return a Budget".
// that's how MediatR knows to send CreateBudgetCommand here.
public sealed class CreateBudgetCommandHandler(IBudgetRepository repository)
    : IRequestHandler<CreateBudgetCommand, Budget>
{
    public async Task<Budget> Handle(CreateBudgetCommand command, CancellationToken cancellationToken)
    {
        // let the Domain create the budget. It checks the limit rules.
        Budget budget = Budget.Create(command.Name, command.Limit);

        // add each expense through the Domain. It checks the amount and over-limit rules.
        // if a rule is broken, a DomainException is thrown here and we never reach Save.
        foreach (NewExpense expense in command.Expenses)
        {
            // no date sent: use today.
            DateOnly date = expense.Date ?? DateOnly.FromDateTime(DateTime.Today);

            budget.AddExpense(expense.Amount, expense.Description, date);
        }

        // every rule passed: save it.
        await repository.Save(budget, cancellationToken);

        return budget;
    }
}
