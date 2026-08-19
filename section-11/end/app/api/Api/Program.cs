using System.Text.Json.Serialization;
using ExpenseTracker.Api.Data;
using ExpenseTracker.Api.Features.Companies;
using ExpenseTracker.Api.Features.Expenses;
using ExpenseTracker.Api.Features.Subscriptions;
using Microsoft.EntityFrameworkCore;

const string DevCorsPolicy = "AllowViteDevServer";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ExpenseDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));

builder.Services.AddProblemDetails();

// Serialize enums as strings on the wire (e.g. "Annual" instead of 1).
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddCors(options =>
        options.AddPolicy(DevCorsPolicy, policy => policy
            .WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod()));
}

var app = builder.Build();

// A body the JSON reader cannot bind (e.g. "date": "") throws BadHttpRequestException,
// which carries its own 400. Without this selector the handler would report it as a 500.
app.UseExceptionHandler(new ExceptionHandlerOptions
{
    StatusCodeSelector = ex => ex is BadHttpRequestException badRequest
        ? badRequest.StatusCode
        : StatusCodes.Status500InternalServerError,
});
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.UseCors(DevCorsPolicy);
}

app.MapGet("/", () => "Hello and welcome to our Expense Tracker API");

app.MapExpenseEndpoints();
app.MapSubscriptionEndpoints();
app.MapCompanyEndpoints();

app.Run();
