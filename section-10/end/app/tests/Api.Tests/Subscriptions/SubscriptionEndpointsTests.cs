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
        DateOnly? renewalDate = null,
        SubscriptionStatus status = SubscriptionStatus.Active) =>
        new(vendor, amount, currency, billingCycle, renewalDate ?? new DateOnly(2026, 8, 1), status);

    private static async Task<SubscriptionResponse> CreateAsync(HttpClient client, CreateSubscriptionRequest request)
    {
        var response = await client.PostAsJsonAsync("/api/subscriptions", request, Json, Ct);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<SubscriptionResponse>(Json, Ct);
        Assert.NotNull(created);
        return created!;
    }

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

    [Fact]
    public async Task Post_Should_Default_Status_To_Active_When_Omitted()
    {
        var client = _factory.CreateClient();

        // Posted as raw JSON without a status member, so this exercises the wire default
        // rather than the record's default parameter.
        var response = await client.PostAsJsonAsync(
            "/api/subscriptions",
            new { vendor = "NoStatus", amount = 10m, currency = "EUR", billingCycle = "Monthly", renewalDate = "2026-09-01" },
            Ct);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<SubscriptionResponse>(Json, Ct);
        Assert.NotNull(created);
        Assert.Equal(SubscriptionStatus.Active, created!.Status);
    }

    [Fact]
    public async Task Put_Status_Should_Update_Status_And_Return_200_When_Valid()
    {
        var client = _factory.CreateClient();
        var created = await CreateAsync(client, ValidRequest(vendor: "Flipped"));
        var bystander = await CreateAsync(client, ValidRequest(vendor: "Untouched", renewalDate: new DateOnly(2026, 9, 1)));
        Assert.Equal(SubscriptionStatus.Active, created.Status);

        var response = await client.PutAsJsonAsync(
            $"/api/subscriptions/{created.Id}/status",
            new UpdateSubscriptionStatusRequest("Cancelled"),
            Json,
            Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<SubscriptionResponse>(Json, Ct);
        Assert.NotNull(updated);
        Assert.Equal(SubscriptionStatus.Cancelled, updated!.Status);
        Assert.Equal(created.Id, updated.Id);
        Assert.Equal(created.Vendor, updated.Vendor);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ExpenseDbContext>();
        var persisted = await db.Subscriptions.SingleAsync(s => s.Id == created.Id, Ct);
        Assert.Equal(SubscriptionStatus.Cancelled, persisted.Status);

        var untouched = await db.Subscriptions.SingleAsync(s => s.Id == bystander.Id, Ct);
        Assert.Equal(SubscriptionStatus.Active, untouched.Status);
    }

    [Fact]
    public async Task Put_Status_Should_Accept_Any_Casing_When_Status_Is_Lowercase()
    {
        var client = _factory.CreateClient();
        var created = await CreateAsync(client, ValidRequest(vendor: "LowerCase"));

        var response = await client.PutAsJsonAsync(
            $"/api/subscriptions/{created.Id}/status",
            new UpdateSubscriptionStatusRequest("cancelled"),
            Json,
            Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<SubscriptionResponse>(Json, Ct);
        Assert.Equal(SubscriptionStatus.Cancelled, updated!.Status);
    }

    [Fact]
    public async Task Put_Status_Should_Return_404_When_Subscription_Missing()
    {
        var client = _factory.CreateClient();
        // A bystander so a handler that ignores the id cannot pass by updating whatever it finds.
        await CreateAsync(client, ValidRequest(vendor: "Bystander"));

        var response = await client.PutAsJsonAsync(
            $"/api/subscriptions/{Guid.NewGuid()}/status",
            new UpdateSubscriptionStatusRequest("Cancelled"),
            Json,
            Ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Put_Status_Should_Return_400_When_Status_Unknown()
    {
        var client = _factory.CreateClient();
        var created = await CreateAsync(client, ValidRequest());

        var response = await client.PutAsJsonAsync(
            $"/api/subscriptions/{created.Id}/status",
            new UpdateSubscriptionStatusRequest("Paused"),
            Json,
            Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblem>(Json, Ct);
        Assert.NotNull(problem);
        Assert.Contains("Status", problem!.Errors.Keys);
    }

    [Fact]
    public async Task Get_Should_Return_Only_Active_When_Filtered_By_Active()
    {
        var client = _factory.CreateClient();
        await SeedMixedStatusesAsync(client);

        var active = await client.GetFromJsonAsync<List<SubscriptionResponse>>("/api/subscriptions?status=Active", Json, Ct);

        Assert.NotNull(active);
        Assert.Equal(2, active!.Count);
        Assert.All(active, s => Assert.Equal(SubscriptionStatus.Active, s.Status));
        Assert.Collection(active,
            s => Assert.Equal("StillActive", s.Vendor),
            s => Assert.Equal("AlsoActive", s.Vendor));
    }

    [Fact]
    public async Task Get_Should_Return_Only_Cancelled_When_Filtered_By_Cancelled()
    {
        var client = _factory.CreateClient();
        await SeedMixedStatusesAsync(client);

        var cancelled = await client.GetFromJsonAsync<List<SubscriptionResponse>>("/api/subscriptions?status=Cancelled", Json, Ct);

        Assert.NotNull(cancelled);
        var single = Assert.Single(cancelled!);
        Assert.Equal("Dropped", single.Vendor);
        Assert.Equal(SubscriptionStatus.Cancelled, single.Status);
    }

    [Fact]
    public async Task Get_Should_Return_Every_Row_When_Filter_Omitted()
    {
        var client = _factory.CreateClient();
        await SeedMixedStatusesAsync(client);

        // The clause that catches a filter quietly returning nothing: an assertion on the
        // filtered result alone passes happily against an empty list.
        var all = await client.GetFromJsonAsync<List<SubscriptionResponse>>("/api/subscriptions", Json, Ct);

        Assert.NotNull(all);
        Assert.Equal(3, all!.Count);
    }

    [Fact]
    public async Task Get_Should_Ignore_Casing_When_Filter_Is_Lowercase()
    {
        var client = _factory.CreateClient();
        await SeedMixedStatusesAsync(client);

        var lower = await client.GetFromJsonAsync<List<SubscriptionResponse>>("/api/subscriptions?status=active", Json, Ct);

        Assert.NotNull(lower);
        Assert.Equal(2, lower!.Count);
        Assert.All(lower, s => Assert.Equal(SubscriptionStatus.Active, s.Status));
    }

    [Fact]
    public async Task Get_Should_Return_400_When_Filter_Status_Unknown()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/subscriptions?status=Paused", Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblem>(Json, Ct);
        Assert.NotNull(problem);
        Assert.Contains("status", problem!.Errors.Keys);
    }

    /// <summary>
    /// Two active rows and one cancelled, ordered so renewal date decides the sequence.
    /// The cancelled one is flipped through the update endpoint rather than created that
    /// way, so the filter is exercised against a status that was actually edited.
    /// </summary>
    private static async Task SeedMixedStatusesAsync(HttpClient client)
    {
        await CreateAsync(client, ValidRequest(vendor: "StillActive", renewalDate: new DateOnly(2026, 8, 1)));
        await CreateAsync(client, ValidRequest(vendor: "AlsoActive", renewalDate: new DateOnly(2026, 9, 1)));
        var toCancel = await CreateAsync(client, ValidRequest(vendor: "Dropped", renewalDate: new DateOnly(2026, 10, 1)));

        var flip = await client.PutAsJsonAsync(
            $"/api/subscriptions/{toCancel.Id}/status",
            new UpdateSubscriptionStatusRequest("Cancelled"),
            Json,
            Ct);
        Assert.Equal(HttpStatusCode.OK, flip.StatusCode);
    }

    private sealed record ValidationProblem(IDictionary<string, string[]> Errors);
}
