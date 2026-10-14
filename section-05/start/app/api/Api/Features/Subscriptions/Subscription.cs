namespace ExpenseTracker.Api.Features.Subscriptions;

// Intended shape for the Subscriptions solo track. This is deliberately NOT wired into
// ExpenseDbContext, any endpoint, or a migration. Build that yourself, section by section.

/// <summary>
/// How often a subscription is billed: every month or once a year.
/// </summary>
public enum BillingCycle
{
    Monthly,
    Annual,
}

public class Subscription
{
    public Guid Id { get; set; }

    public required string Vendor { get; set; }

    public decimal Amount { get; set; }

    public required string Currency { get; set; }

    public BillingCycle BillingCycle { get; set; }

    public DateOnly RenewalDate { get; set; }


    public decimal MonthlyCost()
    {
        if (BillingCycle == BillingCycle.Annual)
        {
            return Amount / 12;
        }

        return Amount;
    }

    public bool IsDueWithin(int days)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        int daysLeft = RenewalDate.DayNumber - today.DayNumber;
        if (daysLeft >= 0 && daysLeft <= days)
        {
            return true;
        }
        else
        {
            return false;
        }
    }


    public string Label() => Vendor + " (" + Currency + ")";
}
