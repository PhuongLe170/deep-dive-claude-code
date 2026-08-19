namespace ExpenseTracker.Api.Features.Expenses;

/// <summary>
/// A recurring monthly spending budget for a single currency. One row per currency;
/// the amount applies to every month and is compared against that currency's totals.
/// </summary>
public class MonthlyBudget
{
    public Guid Id { get; set; }

    public required string Currency { get; set; }

    public decimal Amount { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
