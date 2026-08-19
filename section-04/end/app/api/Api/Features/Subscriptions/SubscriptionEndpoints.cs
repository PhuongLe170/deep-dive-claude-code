using ExpenseTracker.Api.Data;
using ExpenseTracker.Api.Features.Expenses;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Api.Features.Subscriptions;

public static class SubscriptionEndpoints
{
    public static IEndpointRouteBuilder MapSubscriptionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/subscriptions");

        group.MapPost("/", CreateSubscription);
        group.MapGet("/", GetSubscriptions);
        group.MapDelete("/{id:guid}", DeleteSubscription);

        return app;
    }

    private static async Task<IResult> CreateSubscription(
        CreateSubscriptionRequest request,
        ExpenseDbContext db,
        CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();
        void AddError(string field, string message) => errors[field] = [message];

        var vendor = request.Vendor?.Trim();
        if (string.IsNullOrWhiteSpace(vendor))
            AddError(nameof(request.Vendor), "Vendor is required.");
        else if (vendor.Length > 200)
            AddError(nameof(request.Vendor), "Vendor must be 200 characters or fewer.");

        if (request.Amount <= 0)
            AddError(nameof(request.Amount), "Amount must be greater than zero.");

        if (!Currencies.IsAllowed(request.Currency))
            AddError(nameof(request.Currency), $"Currency must be one of: {string.Join(", ", Currencies.Allowed)}.");

        if (!Enum.IsDefined(request.BillingCycle))
            AddError(nameof(request.BillingCycle), "Billing cycle must be Monthly or Annual.");

        if (request.RenewalDate == default)
            AddError(nameof(request.RenewalDate), "Renewal date is required.");

        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        var subscription = request.ToEntity();
        db.Subscriptions.Add(subscription);
        await db.SaveChangesAsync(cancellationToken);

        return Results.Created($"/api/subscriptions/{subscription.Id}", subscription.ToResponse());
    }

    private static async Task<IResult> GetSubscriptions(
        ExpenseDbContext db,
        CancellationToken cancellationToken)
    {
        var subscriptions = await db.Subscriptions
            .AsNoTracking()
            .OrderBy(s => s.RenewalDate)
            .ThenBy(s => s.Vendor)
            .Select(s => new SubscriptionResponse(s.Id, s.Vendor, s.Amount, s.Currency, s.BillingCycle, s.RenewalDate))
            .ToListAsync(cancellationToken);

        return Results.Ok(subscriptions);
    }

    private static async Task<IResult> DeleteSubscription(
        Guid id,
        ExpenseDbContext db,
        CancellationToken cancellationToken)
    {
        var deleted = await db.Subscriptions
            .Where(s => s.Id == id)
            .ExecuteDeleteAsync(cancellationToken);

        return deleted > 0 ? Results.NoContent() : Results.NotFound();
    }
}
