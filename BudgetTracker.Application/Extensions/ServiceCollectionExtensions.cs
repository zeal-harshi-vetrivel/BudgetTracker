using BudgetTracker.Application.Behaviors;          // LoggingBehavior, DomainEventsBehavior
using BudgetTracker.Application.Events;             // IDomainEventCollector, DomainEventCollector
using Microsoft.Extensions.DependencyInjection;      // IServiceCollection

namespace BudgetTracker.Application.Extensions;

/// <summary>
///   Lets the API set up the Application project (MediatR, handlers, behaviors) in one line.
/// </summary>

// same idea as AddDataServices in the Data project: an extension method Program.cs can call.
public static class ServiceCollectionExtensions
{
    // called once at startup in Program.cs.
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddMediatR(config =>
        {
            // scan THIS project for every IRequestHandler (our command handlers)
            // and register each one, so MediatR can find the right handler for a command.
            config.RegisterServicesFromAssembly(typeof(ServiceCollectionExtensions).Assembly);

            // wrap every handler in LoggingBehavior. "<,>" = works for any command and any result type.
            config.AddOpenBehavior(typeof(LoggingBehavior<,>));

            // then DomainEventsBehavior, inside the logging wrapper: sends out events after a command succeeds.
            config.AddOpenBehavior(typeof(DomainEventsBehavior<,>));
        });

        // one collector per HTTP request, shared by the repository (adds events) and DomainEventsBehavior (sends them).
        services.AddScoped<IDomainEventCollector, DomainEventCollector>();

        return services;
    }
}
