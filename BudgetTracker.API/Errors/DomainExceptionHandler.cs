using BudgetTracker.Domain.Core;              // DomainException
using Microsoft.AspNetCore.Diagnostics;       // IExceptionHandler
using Microsoft.AspNetCore.Mvc;               // ProblemDetails

namespace BudgetTracker.API.Errors;

/// <summary>
///   Turns any broken business rule into a 400 response, in one place for every endpoint.
/// </summary>

// replaces the two copies of "catch (DomainException)" that used to be in the controller.
// ASP.NET calls this when an exception escapes a controller action.
public sealed class DomainExceptionHandler(IProblemDetailsService problemDetails) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // not a business rule (a real bug, a database problem...): return false so ASP.NET
        // handles it as a normal 500 error.
        if (exception is not DomainException domainException)
            return false;

        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;

        // same 400 body the controller used to send: title = exception name, detail = its message.
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = domainException.GetType().Name,
                Detail = domainException.Message
            }
        });
    }
}
