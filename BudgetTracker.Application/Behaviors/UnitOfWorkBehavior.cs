using BudgetTracker.Application.Commands;      // CommandBase
using BudgetTracker.Application.UnitOfWorks;   // IUnitOfWork
using MediatR;                                 // IPipelineBehavior, RequestHandlerDelegate

namespace BudgetTracker.Application.Behaviors;

/// <summary>
///   Wraps every command in a unit of work: run the handler, then commit once at the end.
/// </summary>

// based on Chris's UnitOfWorkBehavior (Nov 11 session).
// queries pass straight through. Commands commit only at the outermost level,
// so if a command ever sends another command, everything is saved together once.
public sealed class UnitOfWorkBehavior<TRequest, TResponse>(
    IUnitOfWork unitOfWork,
    UnitOfWorkBehaviorState state)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // a query never changes anything, so there's nothing to commit.
        if (request is not CommandBase)
            return await next();

        // one level deeper (1 = the command the controller sent).
        state.CallLevel++;

        try
        {
            // run the handler. If a rule is broken it throws here, and we never commit,
            // so nothing is saved and no events are sent.
            TResponse response = await next();

            // back at the first command: send the events, then save everything.
            if (state.CallLevel == 1)
                await unitOfWork.Commit(cancellationToken);

            return response;
        }
        finally
        {
            // always step back out, even when the handler threw.
            state.CallLevel--;
        }
    }
}

/// <summary>
///   How many commands deep we are in the current request. One per request (Scoped).
/// </summary>
public sealed class UnitOfWorkBehaviorState
{
    public int CallLevel { get; set; }
}
