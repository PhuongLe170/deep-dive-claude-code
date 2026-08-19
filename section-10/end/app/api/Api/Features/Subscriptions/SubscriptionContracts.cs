using ExpenseTracker.Api.Features.Expenses;

namespace ExpenseTracker.Api.Features.Subscriptions;

// Status is defaulted so every existing construction of this positional record keeps
// compiling, and a caller with no opinion gets an active subscription.
public record CreateSubscriptionRequest(
    string Vendor,
    decimal Amount,
    string Currency,
    BillingCycle BillingCycle,
    DateOnly RenewalDate,
    SubscriptionStatus Status = SubscriptionStatus.Active);

/// <summary>
/// Status arrives as a string so an unknown value is a <c>ValidationProblem</c> from the
/// handler rather than a bare binding failure, and so casing does not matter.
/// </summary>
public record UpdateSubscriptionStatusRequest(string Status);

public record SubscriptionResponse(
    Guid Id,
    string Vendor,
    decimal Amount,
    string Currency,
    BillingCycle BillingCycle,
    DateOnly RenewalDate,
    SubscriptionStatus Status);

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
        Status = request.Status,
    };

    public static SubscriptionResponse ToResponse(this Subscription subscription) => new(
        subscription.Id,
        subscription.Vendor,
        subscription.Amount,
        subscription.Currency,
        subscription.BillingCycle,
        subscription.RenewalDate,
        subscription.Status);
}
