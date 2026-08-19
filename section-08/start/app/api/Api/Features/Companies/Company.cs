namespace ExpenseTracker.Api.Features.Companies;

public class Company
{
    public Guid Id { get; set; }

    public required string Name { get; set; }

    public required string Website { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
