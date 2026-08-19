using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using ExpenseTracker.Api.Data;
using ExpenseTracker.Api.Features.Subscriptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ExpenseTracker.Api.Tests;

public class SubscriptionEndpointsTests(ExpenseApiFactory factory) : IAsyncLifetime
{
    private readonly ExpenseApiFactory _factory = factory;

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    // The API sends enums as strings (JsonStringEnumConverter). CreateClient's HttpClient
    // uses its own defaults, so the test must opt into the same converter or it cannot
    // read "Annual" back into the enum.
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    public ValueTask InitializeAsync() => new(_factory.ResetAsync());

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private static CreateSubscriptionRequest ValidRequest(
        string vendor = "GitHub",
        decimal amount = 21m,
        string currency = "EUR",
        BillingCycle billingCycle = BillingCycle.Monthly,
        DateOnly? renewalDate = null) =>
        new(vendor, amount, currency, billingCycle, renewalDate ?? new DateOnly(2026, 8, 1));

    [Fact]
    public async Task Post_Should_Return_201_And_Persist_When_Valid()
    {
        var client = _factory.CreateClient();
        var request = ValidRequest(vendor: "Figma", amount: 45m, currency: "USD", billingCycle: BillingCycle.Annual);

        var response = await client.PostAsJsonAsync("/api/subscriptions", request, Json, Ct);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        var created = await response.Content.ReadFromJsonAsync<SubscriptionResponse>(Json, Ct);
        Assert.NotNull(created);
        Assert.NotEqual(Guid.Empty, created!.Id);
        Assert.Equal(request.Vendor, created.Vendor);
        Assert.Equal(request.Amount, created.Amount);
        Assert.Equal(request.Currency, created.Currency);
        Assert.Equal(request.BillingCycle, created.BillingCycle);
        Assert.Equal(request.RenewalDate, created.RenewalDate);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ExpenseDbContext>();
        var persisted = await db.Subscriptions.SingleAsync(Ct);
        Assert.Equal(created.Id, persisted.Id);
        Assert.Equal(45m, persisted.Amount);
        Assert.Equal(BillingCycle.Annual, persisted.BillingCycle);
    }

    [Fact]
    public async Task Get_Should_Return_Subscriptions_Ordered_By_RenewalDate_Asc()
    {
        var client = _factory.CreateClient();
        await client.PostAsJsonAsync("/api/subscriptions", ValidRequest(vendor: "Latest", renewalDate: new DateOnly(2026, 12, 1)), Json, Ct);
        await client.PostAsJsonAsync("/api/subscriptions", ValidRequest(vendor: "Soonest", renewalDate: new DateOnly(2026, 8, 1)), Json, Ct);
        await client.PostAsJsonAsync("/api/subscriptions", ValidRequest(vendor: "Middle", renewalDate: new DateOnly(2026, 10, 1)), Json, Ct);

        var subscriptions = await client.GetFromJsonAsync<List<SubscriptionResponse>>("/api/subscriptions", Json, Ct);

        Assert.NotNull(subscriptions);
        Assert.Equal(3, subscriptions!.Count);
        Assert.Collection(subscriptions,
            s => Assert.Equal("Soonest", s.Vendor),
            s => Assert.Equal("Middle", s.Vendor),
            s => Assert.Equal("Latest", s.Vendor));
    }

    [Fact]
    public async Task Delete_Should_Remove_Row_And_Second_Delete_Should_Return_404()
    {
        var client = _factory.CreateClient();
        var post = await client.PostAsJsonAsync("/api/subscriptions", ValidRequest(), Json, Ct);
        var created = await post.Content.ReadFromJsonAsync<SubscriptionResponse>(Json, Ct);
        Assert.NotNull(created);

        var first = await client.DeleteAsync($"/api/subscriptions/{created!.Id}", Ct);
        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);

        var second = await client.DeleteAsync($"/api/subscriptions/{created.Id}", Ct);
        Assert.Equal(HttpStatusCode.NotFound, second.StatusCode);

        var remaining = await client.GetFromJsonAsync<List<SubscriptionResponse>>("/api/subscriptions", Json, Ct);
        Assert.Empty(remaining!);
    }

    [Fact]
    public async Task Post_Should_Return_400_When_Vendor_Empty()
    {
        var client = _factory.CreateClient();
        var request = ValidRequest(vendor: "   ");

        var response = await client.PostAsJsonAsync("/api/subscriptions", request, Json, Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblem>(Json, Ct);
        Assert.NotNull(problem);
        Assert.Contains("Vendor", problem!.Errors.Keys);
    }

    [Fact]
    public async Task Post_Should_Return_400_When_Currency_Unsupported()
    {
        var client = _factory.CreateClient();
        var request = ValidRequest(currency: "XXX");

        var response = await client.PostAsJsonAsync("/api/subscriptions", request, Json, Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblem>(Json, Ct);
        Assert.NotNull(problem);
        Assert.Contains("Currency", problem!.Errors.Keys);
    }

    private sealed record ValidationProblem(IDictionary<string, string[]> Errors);
}
