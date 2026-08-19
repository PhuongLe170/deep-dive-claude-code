namespace ExpenseTracker.Api.Features.Subscriptions;

// The Subscriptions solo track. Registered on ExpenseDbContext, mapped by
// SubscriptionEndpoints, and backed by the AddSubscriptions migration.

public enum BillingCycle
{
    Monthly,
    Annual,
}

public enum SubscriptionStatus
{
    Active,
    Cancelled,
}

public class Subscription
{
    public Guid Id { get; set; }

    public required string Vendor { get; set; }

    public decimal Amount { get; set; }

    public required string Currency { get; set; }

    public BillingCycle BillingCycle { get; set; }

    public DateOnly RenewalDate { get; set; }

    public SubscriptionStatus Status { get; set; }
}
