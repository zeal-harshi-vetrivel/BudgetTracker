using BudgetTracker.API.Errors;              // DomainExceptionHandler
using BudgetTracker.Application.Extensions;  // AddApplicationServices
using BudgetTracker.Data.Extensions;         // AddDataServices, ApplyMigrations

var builder = WebApplication.CreateBuilder(args);

// register controller support so ASP.NET can find classes like BudgetsController
builder.Services.AddControllers();

// keep the OpenAPI doc from the template (lets us see our endpoints at /openapi/v1.json)
builder.Services.AddOpenApi();

// read the "BudgetTracker" connection string from appsettings.json.
// if it's missing, stop right away with a clear message instead of failing later.
string connectionString = builder.Configuration.GetConnectionString("BudgetTracker")
    ?? throw new InvalidOperationException("Connection string 'BudgetTracker' is missing from appsettings.json.");

// register the Data project: SQLite DbContext + IBudgetRepository -> BudgetRepository
builder.Services.AddDataServices(connectionString);

// register the Application project: MediatR, our command handlers, and LoggingBehavior
builder.Services.AddApplicationServices();

// turn broken business rules into 400 ProblemDetails, in one place for every endpoint
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<DomainExceptionHandler>();

var app = builder.Build();

// create the database file / apply any new migrations before handling requests
app.Services.ApplyMigrations();

// catch exceptions from later steps (controllers) and hand them to DomainExceptionHandler
app.UseExceptionHandler();

// only show the OpenAPI doc while developing
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// connect each controller's [Route] attributes to real URLs
app.MapControllers();

app.Run();