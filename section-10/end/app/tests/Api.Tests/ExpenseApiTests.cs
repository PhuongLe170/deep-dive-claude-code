using System.Net;
using System.Net.Http.Json;
using System.Text;
using ExpenseTracker.Api.Data;
using ExpenseTracker.Api.Features.Expenses;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ExpenseTracker.Api.Tests;

public class ExpenseApiTests(ExpenseApiFactory factory) : IAsyncLifetime
{
    private readonly ExpenseApiFactory _factory = factory;

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public ValueTask InitializeAsync() => new(_factory.ResetAsync());

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private static CreateExpenseRequest ValidRequest(
        decimal amount = 12.50m,
        string currency = "EUR",
        string description = "Lunch",
        DateOnly? date = null,
        string? note = null) =>
        new(amount, currency, description, date ?? new DateOnly(2026, 7, 1), note);

    private static UpdateExpenseRequest ValidUpdateRequest(
        decimal amount = 12.50m,
        string currency = "EUR",
        string description = "Lunch",
        DateOnly? date = null,
        string? note = null) =>
        new(amount, currency, description, date ?? new DateOnly(2026, 7, 1), note);

    [Fact]
    public async Task Post_Should_Return_201_And_Persist_When_Valid()
    {
        var client = _factory.CreateClient();
        var request = ValidRequest(amount: 42.99m, currency: "USD", description: "Books");

        var response = await client.PostAsJsonAsync("/api/expenses", request, Ct);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        var created = await response.Content.ReadFromJsonAsync<ExpenseResponse>(Ct);
        Assert.NotNull(created);
        Assert.NotEqual(Guid.Empty, created!.Id);
        Assert.Equal(request.Amount, created.Amount);
        Assert.Equal(request.Currency, created.Currency);
        Assert.Equal(request.Description, created.Description);
        Assert.Equal(request.Date, created.Date);
        Assert.NotEqual(default, created.CreatedAt);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ExpenseDbContext>();
        var persisted = await db.Expenses.SingleAsync(Ct);
        Assert.Equal(created.Id, persisted.Id);
        Assert.Equal(42.99m, persisted.Amount);
    }

    [Fact]
    public async Task Post_Should_Persist_Note_When_Provided()
    {
        var client = _factory.CreateClient();
        var request = ValidRequest(note: "  Reimbursable  ");

        var response = await client.PostAsJsonAsync("/api/expenses", request, Ct);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<ExpenseResponse>(Ct);
        Assert.NotNull(created);
        Assert.Equal("Reimbursable", created!.Note);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ExpenseDbContext>();
        var persisted = await db.Expenses.SingleAsync(Ct);
        Assert.Equal("Reimbursable", persisted.Note);
    }

    [Fact]
    public async Task Post_Should_Null_Note_When_Omitted()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/expenses", ValidRequest(), Ct);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<ExpenseResponse>(Ct);
        Assert.NotNull(created);
        Assert.Null(created!.Note);
    }

    [Fact]
    public async Task Post_Should_Return_400_When_Note_Too_Long()
    {
        var client = _factory.CreateClient();
        var request = ValidRequest(note: new string('x', 1001));

        var response = await client.PostAsJsonAsync("/api/expenses", request, Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblem>(Ct);
        Assert.NotNull(problem);
        Assert.Contains("Note", problem!.Errors.Keys);
    }

    [Fact]
    public async Task Get_Should_Return_Expenses_Ordered_By_Date_Desc()
    {
        var client = _factory.CreateClient();
        await client.PostAsJsonAsync("/api/expenses", ValidRequest(description: "Oldest", date: new DateOnly(2026, 1, 5)), Ct);
        await client.PostAsJsonAsync("/api/expenses", ValidRequest(description: "Newest", date: new DateOnly(2026, 6, 20)), Ct);
        await client.PostAsJsonAsync("/api/expenses", ValidRequest(description: "Middle", date: new DateOnly(2026, 3, 12)), Ct);

        var expenses = await client.GetFromJsonAsync<List<ExpenseResponse>>("/api/expenses", Ct);

        Assert.NotNull(expenses);
        Assert.Equal(3, expenses!.Count);
        Assert.Collection(expenses,
            e => Assert.Equal("Newest", e.Description),
            e => Assert.Equal("Middle", e.Description),
            e => Assert.Equal("Oldest", e.Description));
    }

    [Fact]
    public async Task Delete_Should_Remove_Row_And_Second_Delete_Should_Return_404()
    {
        var client = _factory.CreateClient();
        var post = await client.PostAsJsonAsync("/api/expenses", ValidRequest(), Ct);
        var created = await post.Content.ReadFromJsonAsync<ExpenseResponse>(Ct);
        Assert.NotNull(created);

        var first = await client.DeleteAsync($"/api/expenses/{created!.Id}", Ct);
        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);

        var second = await client.DeleteAsync($"/api/expenses/{created.Id}", Ct);
        Assert.Equal(HttpStatusCode.NotFound, second.StatusCode);

        var remaining = await client.GetFromJsonAsync<List<ExpenseResponse>>("/api/expenses", Ct);
        Assert.Empty(remaining!);
    }

    [Fact]
    public async Task Monthly_Totals_Should_Group_By_Month_And_Currency()
    {
        var client = _factory.CreateClient();
        await client.PostAsJsonAsync("/api/expenses", ValidRequest(amount: 10m, currency: "EUR", date: new DateOnly(2026, 6, 3)), Ct);
        await client.PostAsJsonAsync("/api/expenses", ValidRequest(amount: 15m, currency: "EUR", date: new DateOnly(2026, 6, 18)), Ct);
        await client.PostAsJsonAsync("/api/expenses", ValidRequest(amount: 20m, currency: "USD", date: new DateOnly(2026, 6, 9)), Ct);
        await client.PostAsJsonAsync("/api/expenses", ValidRequest(amount: 5m, currency: "EUR", date: new DateOnly(2026, 5, 1)), Ct);

        var totals = await client.GetFromJsonAsync<List<MonthlyTotalResponse>>("/api/expenses/totals/monthly", Ct);

        Assert.NotNull(totals);
        Assert.Equal(3, totals!.Count);

        // Most recent month first.
        Assert.Equal(2026, totals[0].Year);
        Assert.Equal(6, totals[0].Month);

        var juneEur = totals.Single(t => t is { Year: 2026, Month: 6, Currency: "EUR" });
        Assert.Equal(25m, juneEur.Total);

        var juneUsd = totals.Single(t => t is { Year: 2026, Month: 6, Currency: "USD" });
        Assert.Equal(20m, juneUsd.Total);

        var mayEur = totals.Single(t => t is { Year: 2026, Month: 5, Currency: "EUR" });
        Assert.Equal(5m, mayEur.Total);
    }

    [Fact]
    public async Task Post_Should_Return_400_When_Description_Empty()
    {
        var client = _factory.CreateClient();
        var request = ValidRequest(description: "   ");

        var response = await client.PostAsJsonAsync("/api/expenses", request, Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblem>(Ct);
        Assert.NotNull(problem);
        Assert.Contains("Description", problem!.Errors.Keys);
    }

    [Fact]
    public async Task Post_Should_Return_400_When_Description_Too_Long()
    {
        var client = _factory.CreateClient();
        var request = ValidRequest(description: new string('x', 201));

        var response = await client.PostAsJsonAsync("/api/expenses", request, Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblem>(Ct);
        Assert.NotNull(problem);
        Assert.Contains("Description", problem!.Errors.Keys);
    }

    [Fact]
    public async Task Post_Should_Return_400_When_Currency_Unsupported()
    {
        var client = _factory.CreateClient();
        var request = ValidRequest(currency: "XXX");

        var response = await client.PostAsJsonAsync("/api/expenses", request, Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblem>(Ct);
        Assert.NotNull(problem);
        Assert.Contains("Currency", problem!.Errors.Keys);
    }

    [Fact]
    public async Task Put_Should_Update_Row_And_Return_200_When_Valid()
    {
        var client = _factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync("/api/expenses", ValidRequest(note: "Original note"), Ct);
        var created = await createResponse.Content.ReadFromJsonAsync<ExpenseResponse>(Ct);
        Assert.NotNull(created);

        // A second row the update must leave alone, so the id predicate is pinned.
        var bystanderResponse = await client.PostAsJsonAsync(
            "/api/expenses", ValidRequest(amount: 7.25m, description: "Bystander"), Ct);
        var bystander = await bystanderResponse.Content.ReadFromJsonAsync<ExpenseResponse>(Ct);
        Assert.NotNull(bystander);

        var update = ValidUpdateRequest(
            amount: 99.95m,
            currency: "GBP",
            description: "Client dinner",
            date: new DateOnly(2026, 8, 14),
            note: "Reimbursable");

        var response = await client.PutAsJsonAsync($"/api/expenses/{created!.Id}", update, Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<ExpenseResponse>(Ct);
        Assert.NotNull(updated);
        Assert.Equal(created.Id, updated!.Id);
        Assert.Equal(created.CreatedAt, updated.CreatedAt);
        Assert.Equal(99.95m, updated.Amount);
        Assert.Equal("GBP", updated.Currency);
        Assert.Equal("Client dinner", updated.Description);
        Assert.Equal(new DateOnly(2026, 8, 14), updated.Date);
        Assert.Equal("Reimbursable", updated.Note);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ExpenseDbContext>();
        var persisted = await db.Expenses.SingleAsync(e => e.Id == created.Id, Ct);
        Assert.Equal(created.CreatedAt, persisted.CreatedAt);
        Assert.Equal(99.95m, persisted.Amount);
        Assert.Equal("GBP", persisted.Currency);
        Assert.Equal("Client dinner", persisted.Description);
        Assert.Equal(new DateOnly(2026, 8, 14), persisted.Date);
        Assert.Equal("Reimbursable", persisted.Note);

        var untouched = await db.Expenses.SingleAsync(e => e.Id == bystander!.Id, Ct);
        Assert.Equal(7.25m, untouched.Amount);
        Assert.Equal("Bystander", untouched.Description);
        Assert.Equal(2, await db.Expenses.CountAsync(Ct));
    }

    [Fact]
    public async Task Put_Should_Return_404_When_Expense_Missing()
    {
        var client = _factory.CreateClient();

        // Seed a row so a handler that ignored the id predicate would wrongly find it.
        var createResponse = await client.PostAsJsonAsync("/api/expenses", ValidRequest(description: "Bystander"), Ct);
        var created = await createResponse.Content.ReadFromJsonAsync<ExpenseResponse>(Ct);
        Assert.NotNull(created);

        var response = await client.PutAsJsonAsync($"/api/expenses/{Guid.NewGuid()}", ValidUpdateRequest(), Ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ExpenseDbContext>();
        var untouched = await db.Expenses.SingleAsync(Ct);
        Assert.Equal("Bystander", untouched.Description);
        Assert.Equal(12.50m, untouched.Amount);
    }

    [Fact]
    public async Task Put_Should_Return_400_When_Body_Invalid_And_Expense_Missing()
    {
        var client = _factory.CreateClient();

        // Validation runs before the lookup, so a bad body wins over an unknown id.
        var response = await client.PutAsJsonAsync(
            $"/api/expenses/{Guid.NewGuid()}", ValidUpdateRequest(amount: 0m), Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblem>(Ct);
        Assert.NotNull(problem);
        Assert.Contains("Amount", problem!.Errors.Keys);
    }

    [Fact]
    public async Task Put_Should_Return_400_When_Body_Cannot_Bind()
    {
        var client = _factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync("/api/expenses", ValidRequest(), Ct);
        var created = await createResponse.Content.ReadFromJsonAsync<ExpenseResponse>(Ct);
        Assert.NotNull(created);

        // An empty date cannot bind to DateOnly, so this fails before the handler runs.
        using var body = new StringContent(
            """{"amount":12.50,"currency":"EUR","description":"Lunch","date":""}""",
            Encoding.UTF8,
            "application/json");

        var response = await client.PutAsync($"/api/expenses/{created!.Id}", body, Ct);

        // A malformed body is the caller's fault: 400, never a 500.
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Put_Should_Return_400_When_Amount_Exceeds_Column_Range()
    {
        var client = _factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync("/api/expenses", ValidRequest(), Ct);
        var created = await createResponse.Content.ReadFromJsonAsync<ExpenseResponse>(Ct);
        Assert.NotNull(created);

        var response = await client.PutAsJsonAsync(
            $"/api/expenses/{created!.Id}", ValidUpdateRequest(amount: 10_000_000_000_000_000m), Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblem>(Ct);
        Assert.NotNull(problem);
        Assert.Contains("Amount", problem!.Errors.Keys);
    }

    [Fact]
    public async Task Put_Should_Return_400_When_Amount_Not_Positive()
    {
        var client = _factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync("/api/expenses", ValidRequest(), Ct);
        var created = await createResponse.Content.ReadFromJsonAsync<ExpenseResponse>(Ct);
        Assert.NotNull(created);

        var response = await client.PutAsJsonAsync($"/api/expenses/{created!.Id}", ValidUpdateRequest(amount: 0m), Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblem>(Ct);
        Assert.NotNull(problem);
        Assert.Contains("Amount", problem!.Errors.Keys);
    }

    [Fact]
    public async Task Put_Should_Return_400_When_Description_Empty()
    {
        var client = _factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync("/api/expenses", ValidRequest(), Ct);
        var created = await createResponse.Content.ReadFromJsonAsync<ExpenseResponse>(Ct);
        Assert.NotNull(created);

        var response = await client.PutAsJsonAsync($"/api/expenses/{created!.Id}", ValidUpdateRequest(description: "   "), Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblem>(Ct);
        Assert.NotNull(problem);
        Assert.Contains("Description", problem!.Errors.Keys);
    }

    [Fact]
    public async Task Put_Should_Return_400_When_Description_Too_Long()
    {
        var client = _factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync("/api/expenses", ValidRequest(), Ct);
        var created = await createResponse.Content.ReadFromJsonAsync<ExpenseResponse>(Ct);
        Assert.NotNull(created);

        var response = await client.PutAsJsonAsync(
            $"/api/expenses/{created!.Id}", ValidUpdateRequest(description: new string('x', 201)), Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblem>(Ct);
        Assert.NotNull(problem);
        Assert.Contains("Description", problem!.Errors.Keys);
    }

    [Fact]
    public async Task Put_Should_Return_400_When_Currency_Unsupported()
    {
        var client = _factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync("/api/expenses", ValidRequest(), Ct);
        var created = await createResponse.Content.ReadFromJsonAsync<ExpenseResponse>(Ct);
        Assert.NotNull(created);

        var response = await client.PutAsJsonAsync($"/api/expenses/{created!.Id}", ValidUpdateRequest(currency: "XXX"), Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblem>(Ct);
        Assert.NotNull(problem);
        Assert.Contains("Currency", problem!.Errors.Keys);
    }

    [Fact]
    public async Task Put_Should_Clear_Note_When_Omitted()
    {
        var client = _factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync("/api/expenses", ValidRequest(note: "Reimbursable"), Ct);
        var created = await createResponse.Content.ReadFromJsonAsync<ExpenseResponse>(Ct);
        Assert.NotNull(created);
        Assert.Equal("Reimbursable", created!.Note);

        // Send a body with no "note" member at all, not an explicit null, so this
        // pins the documented "omitting note clears it" contract.
        using var body = new StringContent(
            """{"amount":12.50,"currency":"EUR","description":"Lunch","date":"2026-07-01"}""",
            Encoding.UTF8,
            "application/json");
        var response = await client.PutAsync($"/api/expenses/{created.Id}", body, Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<ExpenseResponse>(Ct);
        Assert.NotNull(updated);
        Assert.Null(updated!.Note);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ExpenseDbContext>();
        var persisted = await db.Expenses.SingleAsync(Ct);
        Assert.Null(persisted.Note);
    }

    [Fact]
    public async Task Put_Should_Trim_Description_And_Note()
    {
        var client = _factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync("/api/expenses", ValidRequest(), Ct);
        var created = await createResponse.Content.ReadFromJsonAsync<ExpenseResponse>(Ct);
        Assert.NotNull(created);

        var update = ValidUpdateRequest(description: "  Team lunch  ", note: "  Split with design  ");

        var response = await client.PutAsJsonAsync($"/api/expenses/{created!.Id}", update, Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<ExpenseResponse>(Ct);
        Assert.NotNull(updated);
        Assert.Equal("Team lunch", updated!.Description);
        Assert.Equal("Split with design", updated.Note);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ExpenseDbContext>();
        var persisted = await db.Expenses.SingleAsync(Ct);
        Assert.Equal("Team lunch", persisted.Description);
        Assert.Equal("Split with design", persisted.Note);
    }

    private sealed record ValidationProblem(IDictionary<string, string[]> Errors);
}
