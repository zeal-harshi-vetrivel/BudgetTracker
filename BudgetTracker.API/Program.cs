using BudgetTracker.Data.Extensions;   // AddDataServices, ApplyMigrations

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

var app = builder.Build();

// create the database file / apply any new migrations before handling requests
app.Services.ApplyMigrations();

// only show the OpenAPI doc while developing
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// connect each controller's [Route] attributes to real URLs
app.MapControllers();

app.Run();