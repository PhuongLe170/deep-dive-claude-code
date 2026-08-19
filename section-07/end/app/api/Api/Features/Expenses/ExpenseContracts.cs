namespace ExpenseTracker.Api.Features.Expenses;

public record CreateExpenseRequest(
    decimal Amount,
    string Currency,
    string Description,
    DateOnly Date,
    string? Note = null);

public record ExpenseResponse(
    Guid Id,
    decimal Amount,
    string Currency,
    string Description,
    DateOnly Date,
    DateTimeOffset CreatedAt,
    string? Note);

public record MonthlyTotalResponse(
    int Year,
    int Month,
    string Currency,
    decimal Total);

public record SetBudgetRequest(
    string Currency,
    decimal Amount);

public record BudgetResponse(
    Guid Id,
    string Currency,
    decimal Amount,
    DateTimeOffset UpdatedAt);

public record BudgetComparisonResponse(
    int Year,
    int Month,
    string Currency,
    decimal Spent,
    decimal? Budget,
    decimal? Remaining);

/// <summary>
/// Explicit mapping between the entity and its DTOs. No AutoMapper by design.
/// </summary>
public static class ExpenseMapping
{
    public static Expense ToEntity(this CreateExpenseRequest request) => new()
    {
        Id = Guid.NewGuid(),
        Amount = request.Amount,
        Currency = request.Currency,
        Description = request.Description.Trim(),
        Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim(),
        Date = request.Date,
    };

    public static ExpenseResponse ToResponse(this Expense expense) => new(
        expense.Id,
        expense.Amount,
        expense.Currency,
        expense.Description,
        expense.Date,
        expense.CreatedAt,
        expense.Note);

    public static BudgetResponse ToResponse(this MonthlyBudget budget) => new(
        budget.Id,
        budget.Currency,
        budget.Amount,
        budget.UpdatedAt);
}
