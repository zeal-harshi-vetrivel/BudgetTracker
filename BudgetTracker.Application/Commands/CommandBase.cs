using MediatR;   // IRequest

namespace BudgetTracker.Application.Commands;

/// <summary>
///   Base for every command (anything that may change state). Queries don't use it.
/// </summary>

// UnitOfWorkBehavior checks "request is CommandBase" to decide whether to commit.
// queries skip the commit, because they never change anything.
// same idea as CommandBase in Chris's repo (he uses classes; these are records to match our commands).
public abstract record CommandBase;

/// <summary>
///   Base for a command that returns a result of type TResponse.
/// </summary>
public abstract record CommandBase<TResponse> : CommandBase, IRequest<TResponse>;
