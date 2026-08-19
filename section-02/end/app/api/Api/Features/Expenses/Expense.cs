namespace ExpenseTracker.Api.Features.Expenses;

/// <summary>
/// A single recorded expense. This is the one working entity in the day-one baseline.
/// </summary>
public class Expense
{
    public Guid Id { get; set; }

    public decimal Amount { get; set; }

    public required string Currency { get; set; }

    public required string Description { get; set; }

    public DateOnly Date { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
