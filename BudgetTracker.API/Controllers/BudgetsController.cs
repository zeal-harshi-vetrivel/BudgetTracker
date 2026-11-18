using BudgetTracker.API.Contracts.Requests;              // CreateBudgetRequest, CreateExpenseRequest
using BudgetTracker.API.Contracts.Responses;             // BudgetResponse
using BudgetTracker.Application.Budgets.Commands;        // CreateBudgetCommand, AddExpenseCommand, NewExpense
using BudgetTracker.Application.Data.Repositories;       // IBudgetRepository (GET only, until Week 8)
using BudgetTracker.Domain.Budgets;                      // Budget
using MediatR;                                           // ISender
using Microsoft.AspNetCore.Mvc;                          // ControllerBase, [ApiController], [HttpPost], [HttpGet]

namespace BudgetTracker.API.Controllers;

/// <summary>
///   HTTP endpoints for budgets. Turns HTTP into commands and results back into HTTP. No business rules here.
/// </summary>

// ISender = MediatR's "send a command" door. The controller doesn't know which handler runs.
// IBudgetRepository stays only for the GET until Week 8 turns it into a query.
// broken rules (DomainException) are turned into 400s by DomainExceptionHandler, so no try/catch here.
[ApiController]
[Route("api/[controller]")]
public sealed class BudgetsController(ISender sender, IBudgetRepository repository) : ControllerBase
{
    // POST /api/budgets
    [HttpPost]
    public async Task<ActionResult<BudgetResponse>> Create(
        [FromBody] CreateBudgetRequest request,
        CancellationToken cancellationToken)
    {
        // 1. translate the HTTP request into a command (HTTP classes stay in the API).
        var command = new CreateBudgetCommand(
            request.Name,
            request.Limit,
            request.Expenses.Select(ToNewExpense).ToList());

        // 2. send it. MediatR runs LoggingBehavior, then CreateBudgetCommandHandler.
        Budget budget = await sender.Send(command, cancellationToken);

        // 3. translate the result into HTTP: 201 + Location header + the budget.
        return CreatedAtAction(nameof(GetById), new { id = budget.Id }, BudgetResponse.From(budget));
    }

    // GET /api/budgets/{id}
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BudgetResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        // reading doesn't change anything, so it's not a command. Week 8 replaces this with a query.
        Budget? budget = await repository.Get(id, cancellationToken);

        if (budget is null)
            return NotFound();

        return Ok(BudgetResponse.From(budget));
    }

    // POST /api/budgets/{id}/expenses
    [HttpPost("{id:guid}/expenses")]
    public async Task<ActionResult<BudgetResponse>> AddExpense(
        Guid id,
        [FromBody] CreateExpenseRequest request,
        CancellationToken cancellationToken)
    {
        var command = new AddExpenseCommand(id, ToNewExpense(request));

        Budget? budget = await sender.Send(command, cancellationToken);

        // the handler returns null when no budget has that Id.
        if (budget is null)
            return NotFound();

        return Ok(BudgetResponse.From(budget));
    }

    // HTTP expense -> command expense. Keeps the HTTP class out of the Application layer.
    private static NewExpense ToNewExpense(CreateExpenseRequest expense) =>
        new(expense.Amount, expense.Description, expense.Date);
}
