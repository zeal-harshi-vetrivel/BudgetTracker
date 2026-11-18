using System.Diagnostics;                         // Stopwatch
using BudgetTracker.Domain.Core;                  // DomainException
using MediatR;                                    // IPipelineBehavior, RequestHandlerDelegate
using Microsoft.Extensions.Logging;               // ILogger

namespace BudgetTracker.Application.Behaviors;

/// <summary>
///   Runs around every command: logs when it starts, how long it took, and whether a rule was broken.
/// </summary>

// a pipeline behavior is like a wrapper around the handler.
// MediatR calls Handle here first; "next" is the handler (or the next wrapper in line).
public sealed class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // the command's class name, e.g. "CreateBudgetCommand"
        string name = typeof(TRequest).Name;

        logger.LogInformation("Handling {Command}", name);
        Stopwatch timer = Stopwatch.StartNew();

        try
        {
            // run the handler. Everything before this line happens BEFORE the handler, everything after happens AFTER.
            TResponse response = await next();

            logger.LogInformation("Handled {Command} in {Elapsed} ms", name, timer.ElapsedMilliseconds);
            return response;
        }
        catch (DomainException ex)
        {
            // a business rule said no. That's expected, so it's a warning, not an error.
            logger.LogWarning("{Command} rejected after {Elapsed} ms: {Rule} - {Message}",
                name, timer.ElapsedMilliseconds, ex.GetType().Name, ex.Message);

            // "throw;" passes the same exception on, so the API still turns it into a 400.
            throw;
        }
    }
}
