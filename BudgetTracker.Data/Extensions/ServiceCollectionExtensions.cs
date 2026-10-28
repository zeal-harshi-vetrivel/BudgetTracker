using BudgetTracker.Application.Data.Repositories;   // IBudgetRepository
using BudgetTracker.Data.Repositories;               // BudgetRepository
using Microsoft.EntityFrameworkCore;                 // UseSqlite, Migrate
using Microsoft.Extensions.DependencyInjection;      // IServiceCollection, AddScoped

namespace BudgetTracker.Data.Extensions;

/// <summary>
///   Lets the API set up the Data project without seeing its internal classes.
/// </summary>

// "static class" + "this" on the first parameter = extension methods.
// they let us write builder.Services.AddDataServices(...) as if it were built in.
public static class ServiceCollectionExtensions
{
    // called once at startup in Program.cs.
    public static IServiceCollection AddDataServices(this IServiceCollection services, string connectionString)
    {
        // register our DbContext and tell it to use SQLite at this connection string.
        services.AddDbContext<BudgetTrackerContext>(options => options.UseSqlite(connectionString));

        // whenever something asks for IBudgetRepository, create a BudgetRepository.
        // Scoped = one per HTTP request (same lifetime as the DbContext).
        services.AddScoped<IBudgetRepository, BudgetRepository>();

        return services;
    }

    // called once at startup, after Build(). Creates the database file or brings it up to date.
    public static void ApplyMigrations(this IServiceProvider services)
    {
        // DbContext is "scoped", so we open a temporary scope to get one outside a request.
        using IServiceScope scope = services.CreateScope();

        // run any migrations that haven't been applied yet.
        scope.ServiceProvider.GetRequiredService<BudgetTrackerContext>().Database.Migrate();
    }
}