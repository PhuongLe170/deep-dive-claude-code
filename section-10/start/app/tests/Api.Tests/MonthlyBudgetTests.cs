using System.Net;
using System.Net.Http.Json;
using ExpenseTracker.Api.Data;
using ExpenseTracker.Api.Features.Expenses;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ExpenseTracker.Api.Tests;

public class MonthlyBudgetTests(ExpenseApiFactory factory) : IAsyncLifetime
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
    public async Task Put_Should_Create_Budget_When_New_Currency()
    {
        var client = _factory.CreateClient();

        var response = await client.PutAsJsonAsync("/api/expenses/budgets", new SetBudgetRequest("USD", 2000m), Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<BudgetResponse>(Ct);
        Assert.NotNull(created);
        Assert.NotEqual(Guid.Empty, created!.Id);
        Assert.Equal("USD", created.Currency);
        Assert.Equal(2000m, created.Amount);
        Assert.NotEqual(default, created.UpdatedAt);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ExpenseDbContext>();
        var persisted = await db.MonthlyBudgets.SingleAsync(Ct);
        Assert.Equal(created.Id, persisted.Id);
        Assert.Equal(2000m, persisted.Amount);
        Assert.NotEqual(default, persisted.UpdatedAt);
    }

    [Fact]
    public async Task Put_Should_Update_Budget_When_Currency_Exists()
    {
        var client = _factory.CreateClient();

        var first = await client.PutAsJsonAsync("/api/expenses/budgets", new SetBudgetRequest("USD", 2000m), Ct);
        var firstBudget = await first.Content.ReadFromJsonAsync<BudgetResponse>(Ct);
        Assert.NotNull(firstBudget);

        var second = await client.PutAsJsonAsync("/api/expenses/budgets", new SetBudgetRequest("USD", 3500m), Ct);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        var updated = await second.Content.ReadFromJsonAsync<BudgetResponse>(Ct);
        Assert.NotNull(updated);
        Assert.Equal(firstBudget!.Id, updated!.Id);
        Assert.Equal(3500m, updated.Amount);
        Assert.True(updated.UpdatedAt >= firstBudget.UpdatedAt);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ExpenseDbContext>();
        var persisted = await db.MonthlyBudgets.SingleAsync(Ct);
        Assert.Equal(3500m, persisted.Amount);
    }

    [Fact]
    public async Task Put_Should_Return_400_When_Amount_Not_Positive()
    {
        var client = _factory.CreateClient();

        var response = await client.PutAsJsonAsync("/api/expenses/budgets", new SetBudgetRequest("USD", 0m), Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblem>(Ct);
        Assert.NotNull(problem);
        Assert.Contains("Amount", problem!.Errors.Keys);
    }

    [Fact]
    public async Task Put_Should_Return_400_When_Currency_Unsupported()
    {
        var client = _factory.CreateClient();

        var response = await client.PutAsJsonAsync("/api/expenses/budgets", new SetBudgetRequest("XXX", 100m), Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblem>(Ct);
        Assert.NotNull(problem);
        Assert.Contains("Currency", problem!.Errors.Keys);
    }

    [Fact]
    public async Task Get_Should_Return_Budgets_Ordered_By_Currency()
    {
        var client = _factory.CreateClient();
        await client.PutAsJsonAsync("/api/expenses/budgets", new SetBudgetRequest("USD", 2000m), Ct);
        await client.PutAsJsonAsync("/api/expenses/budgets", new SetBudgetRequest("EUR", 1500m), Ct);
        await client.PutAsJsonAsync("/api/expenses/budgets", new SetBudgetRequest("GBP", 1000m), Ct);

        var budgets = await client.GetFromJsonAsync<List<BudgetResponse>>("/api/expenses/budgets", Ct);

        Assert.NotNull(budgets);
        Assert.Equal(3, budgets!.Count);
        Assert.Collection(budgets,
            b => Assert.Equal("EUR", b.Currency),
            b => Assert.Equal("GBP", b.Currency),
            b => Assert.Equal("USD", b.Currency));
    }

    [Fact]
    public async Task Comparison_Should_Return_Spent_And_Remaining_Per_Month_And_Currency()
    {
        var client = _factory.CreateClient();
        await client.PutAsJsonAsync("/api/expenses/budgets", new SetBudgetRequest("USD", 100m), Ct);
        await client.PostAsJsonAsync("/api/expenses", ValidExpense(30m, "USD", new DateOnly(2026, 6, 3)), Ct);
        await client.PostAsJsonAsync("/api/expenses", ValidExpense(20m, "USD", new DateOnly(2026, 6, 18)), Ct);
        await client.PostAsJsonAsync("/api/expenses", ValidExpense(40m, "EUR", new DateOnly(2026, 6, 9)), Ct);

        var comparison = await client.GetFromJsonAsync<List<BudgetComparisonResponse>>("/api/expenses/budgets/comparison", Ct);

        Assert.NotNull(comparison);

        var juneUsd = comparison!.Single(c => c is { Year: 2026, Month: 6, Currency: "USD" });
        Assert.Equal(50m, juneUsd.Spent);
        Assert.Equal(100m, juneUsd.Budget);
        Assert.Equal(50m, juneUsd.Remaining);

        var juneEur = comparison.Single(c => c is { Year: 2026, Month: 6, Currency: "EUR" });
        Assert.Equal(40m, juneEur.Spent);
        Assert.Null(juneEur.Budget);
        Assert.Null(juneEur.Remaining);
    }

    [Fact]
    public async Task Comparison_Should_Return_Negative_Remaining_When_Overspent()
    {
        var client = _factory.CreateClient();
        await client.PutAsJsonAsync("/api/expenses/budgets", new SetBudgetRequest("USD", 50m), Ct);
        await client.PostAsJsonAsync("/api/expenses", ValidExpense(60m, "USD", new DateOnly(2026, 6, 3)), Ct);
        await client.PostAsJsonAsync("/api/expenses", ValidExpense(15m, "USD", new DateOnly(2026, 6, 18)), Ct);

        var comparison = await client.GetFromJsonAsync<List<BudgetComparisonResponse>>("/api/expenses/budgets/comparison", Ct);

        Assert.NotNull(comparison);
        var juneUsd = comparison!.Single(c => c is { Year: 2026, Month: 6, Currency: "USD" });
        Assert.Equal(75m, juneUsd.Spent);
        Assert.Equal(50m, juneUsd.Budget);
        Assert.Equal(-25m, juneUsd.Remaining);
        Assert.True(juneUsd.Remaining < 0);
    }

    private sealed record ValidationProblem(IDictionary<string, string[]> Errors);
}
