using BudgetTracker.API.Contracts.Requests;              // CreateBudgetRequest
using BudgetTracker.API.Contracts.Responses;             // BudgetResponse
using BudgetTracker.Application.Data.Repositories;       // IBudgetRepository
using BudgetTracker.Domain.Budgets;                      // Budget
using BudgetTracker.Domain.Core;                         // DomainException
using Microsoft.AspNetCore.Mvc;                          // ControllerBase, [ApiController], [HttpPost], [HttpGet]

namespace BudgetTracker.API.Controllers;

/// <summary>
///   HTTP endpoints for budgets. Temporary home for this logic until Week 7 moves it into commands.
/// </summary>

// (IBudgetRepository repository) = ASP.NET hands us a BudgetRepository here,
// because we registered it in AddDataServices. The controller only knows the interface.
[ApiController]
[Route("api/[controller]")]
public sealed class BudgetsController(IBudgetRepository repository) : ControllerBase
{
    // POST /api/budgets
    // creates a budget, adds any starting expenses, and saves it.
    [HttpPost]
    public async Task<ActionResult<BudgetResponse>> Create(
        [FromBody] CreateBudgetRequest request,
        CancellationToken cancellationToken)
    {
        Budget budget;

        try
        {
            // let the Domain create the budget. It checks the limit rules.
            budget = Budget.Create(request.Name, request.Limit);

            // add each expense through the Domain. It checks the amount and over-limit rules.
            foreach (CreateExpenseRequest expense in request.Expenses)
            {
                // if the client didn't send a date, use today.
                DateOnly date = expense.Date ?? DateOnly.FromDateTime(DateTime.Today);

                budget.AddExpense(expense.Amount, expense.Description, date);
            }
        }
        catch (DomainException ex)
        {
            // a business rule was broken. Nothing gets saved. Send a standard 400 error body.
            return Problem(
                title: ex.GetType().Name,
                detail: ex.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }

        // every rule passed: save it to the database.
        await repository.Save(budget, cancellationToken);

        // 201 Created + a Location header pointing at GET /api/budgets/{id} + the budget in the body.
        return CreatedAtAction(nameof(GetById), new { id = budget.Id }, BudgetResponse.From(budget));
    }

    // GET /api/budgets/{id}
    // loads one saved budget. {id:guid} = only match if the id looks like a Guid.
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BudgetResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        // ask the repository. It rebuilds the Budget from the database (Spent recalculated).
        Budget? budget = await repository.Get(id, cancellationToken);

        // no budget with that Id: 404 Not Found.
        if (budget is null)
            return NotFound();

        return Ok(BudgetResponse.From(budget));
    }
}