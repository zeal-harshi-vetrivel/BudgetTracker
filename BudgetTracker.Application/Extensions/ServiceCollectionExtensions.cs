using BudgetTracker.Application.Behaviors;          // LoggingBehavior, UnitOfWorkBehavior, UnitOfWorkBehaviorState
using BudgetTracker.Application.Events;             // IDomainEventCollector, DomainEventCollector
using BudgetTracker.Application.UnitOfWorks;        // IUnitOfWork, UnitOfWork
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
            // scan THIS project for every handler (commands, queries, event handlers)
            // and register each one, so MediatR can find the right handler.
            config.RegisterServicesFromAssembly(typeof(ServiceCollectionExtensions).Assembly);

            // behaviors run in the order they're added (outermost first):
            // LoggingBehavior wraps everything, so it also logs the commit and the event reactions.
            config.AddOpenBehavior(typeof(LoggingBehavior<,>));

            // then UnitOfWorkBehavior: for commands, runs the handler, then sends events and saves once.
            config.AddOpenBehavior(typeof(UnitOfWorkBehavior<,>));
        });

        // all Scoped = one per HTTP request, shared by everything in that request:
        services.AddScoped<UnitOfWorkBehaviorState>();                        // how many commands deep we are
        services.AddScoped<IUnitOfWork, UnitOfWork>();                        // dispatch events, then save
        services.AddScoped<IDomainEventCollector, DomainEventCollector>();    // repository puts events in, unit of work takes them out

        return services;
    }
}
