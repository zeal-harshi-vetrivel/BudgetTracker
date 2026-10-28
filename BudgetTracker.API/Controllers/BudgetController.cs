using BudgetTracker.API.Contracts.Requests;    // CreateBudgetRequest
using BudgetTracker.API.Contracts.Responses;   // BudgetResponse
using BudgetTracker.Domain.Budgets;            // Budget
using BudgetTracker.Domain.Core;               // DomainException
using Microsoft.AspNetCore.Mvc;                // ControllerBase, [ApiController], [HttpPost]

namespace BudgetTracker.API.Controllers;

//   HTTP endpoints for budgets (Temporary)

// [ApiController] turns on helpful defaults, like automatic 400s when the JSON is missing required fields.
// [Route] sets the URL: "api/[controller]" becomes "api/budgets" (the class name minus "Controller").
[ApiController]
[Route("api/[controller]")]
public sealed class BudgetsController : ControllerBase
{
    // POST /api/budgets
    // creates a budget and adds any starting expenses.
    [HttpPost]
    public ActionResult<BudgetResponse> Create([FromBody] CreateBudgetRequest request)
    {
        try
        {
            // let the Domain create the budget. It checks the limit rules.
            Budget budget = Budget.Create(request.Name, request.Limit);

            // add each expense through the Domain. It checks the amount and over-limit rules.
            foreach (CreateExpenseRequest expense in request.Expenses)
            {
                // if the client didn't send a date, use today.
                DateOnly date = expense.Date ?? DateOnly.FromDateTime(DateTime.Today);

                budget.AddExpense(expense.Amount, expense.Description, date);
            }

            // everything passed: turn the Domain object into the response shape.
            return Ok(BudgetResponse.From(budget));
        }
        catch (DomainException ex)
        {
            // a business rule was broken. Send a standard 400 error body (ProblemDetails).
            // title = the exception's class name, e.g. "BudgetExceededException"
            // detail = the message we wrote in the Domain, e.g. "...Only $28.85 remains."
            return Problem(
                title: ex.GetType().Name,
                detail: ex.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }
    }
}