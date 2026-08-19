using System.Net;
using System.Net.Http.Json;
using ExpenseTracker.Api.Features.Expenses;

namespace ExpenseTracker.Api.Tests;

public class MonthlyTotalsTests(ExpenseApiFactory factory) : IAsyncLifetime
{
    private readonly ExpenseApiFactory _factory = factory;

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public ValueTask InitializeAsync() => new(_factory.ResetAsync());

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private static CreateExpenseRequest ValidExpense(
        decimal amount,
        string currency,
        DateOnly date) =>
        new(amount, currency, "Lunch", date, null);

    [Fact]
    public async Task Get_Should_Return_400_When_Month_In_Future()
    {
        var client = _factory.CreateClient();
        var future = DateOnly.FromDateTime(DateTimeOffset.UtcNow.UtcDateTime).AddMonths(1);

        var response = await client.GetAsync(
            $"/api/expenses/totals/monthly?year={future.Year}&month={future.Month}", Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var problem = await response.Content.ReadFromJsonAsync<ValidationProblem>(Ct);
        Assert.NotNull(problem);
        Assert.Equal(400, problem!.Status);
        Assert.False(string.IsNullOrWhiteSpace(problem.Title));
        Assert.Contains("month", problem.Errors.Keys);
        Assert.Contains("Month cannot be in the future.", problem.Errors["month"]);
    }

    [Fact]
    public async Task Get_Should_Return_Totals_When_Month_Is_Current()
    {
        var client = _factory.CreateClient();
        var today = DateOnly.FromDateTime(DateTimeOffset.UtcNow.UtcDateTime);
        await client.PostAsJsonAsync("/api/expenses", ValidExpense(10m, "EUR", today), Ct);

        var response = await client.GetAsync(
            $"/api/expenses/totals/monthly?year={today.Year}&month={today.Month}", Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var totals = await response.Content.ReadFromJsonAsync<List<MonthlyTotalResponse>>(Ct);
        Assert.NotNull(totals);
        var current = Assert.Single(totals!);
        Assert.Equal(today.Year, current.Year);
        Assert.Equal(today.Month, current.Month);
        Assert.Equal(10m, current.Total);
    }

    [Fact]
    public async Task Get_Should_Filter_To_Requested_Month_When_Past()
    {
        var client = _factory.CreateClient();
        await client.PostAsJsonAsync("/api/expenses", ValidExpense(10m, "EUR", new DateOnly(2026, 5, 3)), Ct);
        await client.PostAsJsonAsync("/api/expenses", ValidExpense(15m, "EUR", new DateOnly(2026, 5, 18)), Ct);
        await client.PostAsJsonAsync("/api/expenses", ValidExpense(20m, "EUR", new DateOnly(2026, 6, 9)), Ct);

        var totals = await client.GetFromJsonAsync<List<MonthlyTotalResponse>>(
            "/api/expenses/totals/monthly?year=2026&month=5", Ct);

        Assert.NotNull(totals);
        var may = Assert.Single(totals!);
        Assert.Equal(2026, may.Year);
        Assert.Equal(5, may.Month);
        Assert.Equal(25m, may.Total);
    }

    [Fact]
    public async Task Get_Should_Return_400_When_Month_Out_Of_Range()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/expenses/totals/monthly?year=2026&month=13", Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblem>(Ct);
        Assert.NotNull(problem);
        Assert.Contains("month", problem!.Errors.Keys);
    }

    [Fact]
    public async Task Get_Should_Return_400_When_Year_Supplied_Without_Month()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/expenses/totals/monthly?year=2026", Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblem>(Ct);
        Assert.NotNull(problem);
        Assert.Contains("month", problem!.Errors.Keys);
    }

    private sealed record ValidationProblem(
        string? Title,
        int? Status,
        IDictionary<string, string[]> Errors);
}
