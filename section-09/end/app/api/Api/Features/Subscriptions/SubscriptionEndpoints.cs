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
        group.MapPut("/{id:guid}/status", UpdateSubscriptionStatus);
        group.MapDelete("/{id:guid}", DeleteSubscription);

        return app;
    }

    /// <summary>
    /// Parses a status supplied as text. Accepts any casing and rejects the numeric
    /// strings <see cref="Enum.TryParse{TEnum}(string, bool, out TEnum)"/> would otherwise
    /// let through.
    /// </summary>
    private static bool TryParseStatus(string? value, out SubscriptionStatus status) =>
        Enum.TryParse(value, ignoreCase: true, out status) && Enum.IsDefined(status);

    private static string StatusMessage() =>
        $"Status must be one of: {string.Join(", ", Enum.GetNames<SubscriptionStatus>())}.";

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

        if (!Enum.IsDefined(request.Status))
            AddError(nameof(request.Status), StatusMessage());

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
        CancellationToken cancellationToken,
        string? status = null)
    {
        SubscriptionStatus? filter = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!TryParseStatus(status, out var parsed))
            {
                // Lowercase key: it names the query parameter, as monthly totals does.
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    [nameof(status)] = [StatusMessage()],
                });
            }

            filter = parsed;
        }

        var query = db.Subscriptions.AsNoTracking();
        if (filter is not null)
        {
            query = query.Where(s => s.Status == filter.Value);
        }

        var subscriptions = await query
            .OrderBy(s => s.RenewalDate)
            .ThenBy(s => s.Vendor)
            .Select(s => new SubscriptionResponse(s.Id, s.Vendor, s.Amount, s.Currency, s.BillingCycle, s.RenewalDate, s.Status))
            .ToListAsync(cancellationToken);

        return Results.Ok(subscriptions);
    }

    private static async Task<IResult> UpdateSubscriptionStatus(
        Guid id,
        UpdateSubscriptionStatusRequest request,
        ExpenseDbContext db,
        CancellationToken cancellationToken)
    {
        // Validate before the lookup so a bad body is a 400 whether or not the id exists.
        if (!TryParseStatus(request.Status, out var status))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                [nameof(request.Status)] = [StatusMessage()],
            });
        }

        // Tracked read: EF writes only the Status column and leaves the rest alone.
        var subscription = await db.Subscriptions
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (subscription is null)
        {
            return Results.NotFound();
        }

        subscription.Status = status;
        await db.SaveChangesAsync(cancellationToken);

        return Results.Ok(subscription.ToResponse());
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
