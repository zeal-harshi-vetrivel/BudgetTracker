var builder = WebApplication.CreateBuilder(args);

// register controller support so ASP.NET can find classes like BudgetsController
builder.Services.AddControllers();

// keep the OpenAPI doc from the template (lets us see our endpoints at /openapi/v1.json)
builder.Services.AddOpenApi();

var app = builder.Build();

// only show the OpenAPI doc while developing
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// connect each controller's [Route] attributes to real URLs
app.MapControllers();

app.Run();