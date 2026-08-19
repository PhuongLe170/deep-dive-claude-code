using ExpenseTracker.Api.Features.Expenses;

namespace ExpenseTracker.Api.Features.Subscriptions;

public record CreateSubscriptionRequest(
    string Vendor,
    decimal Amount,
    string Currency,
    BillingCycle BillingCycle,
    DateOnly RenewalDate);

public record SubscriptionResponse(
    Guid Id,
    string Vendor,
    decimal Amount,
    string Currency,
    BillingCycle BillingCycle,
    DateOnly RenewalDate);

/// <summary>
/// Explicit mapping between the entity and its DTOs. No AutoMapper by design.
/// Currency handling is shared with the Expenses slice (see <see cref="Currencies"/>).
/// </summary>
public static class SubscriptionMapping
{
    public static Subscription ToEntity(this CreateSubscriptionRequest request) => new()
    {
        Id = Guid.NewGuid(),
        Vendor = request.Vendor.Trim(),
        Amount = request.Amount,
        Currency = request.Currency,
        BillingCycle = request.BillingCycle,
        RenewalDate = request.RenewalDate,
    };

    public static SubscriptionResponse ToResponse(this Subscription subscription) => new(
        subscription.Id,
        subscription.Vendor,
        subscription.Amount,
        subscription.Currency,
        subscription.BillingCycle,
        subscription.RenewalDate);
}
