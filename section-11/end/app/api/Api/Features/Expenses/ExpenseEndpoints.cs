using ExpenseTracker.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Api.Features.Expenses;

public static class ExpenseEndpoints
{
    /// <summary>
    /// Largest value the numeric(18,2) Amount column holds. Anything above it reaches
    /// Postgres as a 22003 overflow, which would surface as a 500 rather than a 400.
    /// </summary>
    private const decimal MaxAmount = 9_999_999_999_999_999.99m;

    public static IEndpointRouteBuilder MapExpenseEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/expenses");

        group.MapPost("/", CreateExpense);
        group.MapGet("/", GetExpenses);
        group.MapPut("/{id:guid}", UpdateExpense);
        group.MapDelete("/{id:guid}", DeleteExpense);
        group.MapGet("/totals/monthly", GetMonthlyTotals);

        group.MapGet("/budgets", GetBudgets);
        group.MapPut("/budgets", SetBudget);
        group.MapGet("/budgets/comparison", GetBudgetComparison);

        return app;
    }

    private static async Task<IResult> CreateExpense(
        CreateExpenseRequest request,
        ExpenseDbContext db,
        CancellationToken cancellationToken)
    {
        var errors = ValidateExpense(request.Amount, request.Currency, request.Description, request.Date, request.Note);
        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        var expense = request.ToEntity();
        db.Expenses.Add(expense);
        await db.SaveChangesAsync(cancellationToken);

        return Results.Created($"/api/expenses/{expense.Id}", expense.ToResponse());
    }

    /// <summary>
    /// Shared by create and update, which accept the same fields under the same rules.
    /// The keys stay PascalCase because they are part of the published 400 contract.
    /// </summary>
    private static Dictionary<string, string[]> ValidateExpense(
        decimal amount,
        string? currency,
        string? description,
        DateOnly date,
        string? note)
    {
        var errors = new Dictionary<string, string[]>();
        void AddError(string field, string message) => errors[field] = [message];

        if (amount <= 0)
            AddError(nameof(CreateExpenseRequest.Amount), "Amount must be greater than zero.");
        else if (amount > MaxAmount)
            AddError(nameof(CreateExpenseRequest.Amount), $"Amount must be {MaxAmount} or less.");

        var trimmedDescription = description?.Trim();
        if (string.IsNullOrWhiteSpace(trimmedDescription))
            AddError(nameof(CreateExpenseRequest.Description), "Description is required.");
        else if (trimmedDescription.Length > 200)
            AddError(nameof(CreateExpenseRequest.Description), "Description must be 200 characters or fewer.");

        if (!Currencies.IsAllowed(currency))
            AddError(nameof(CreateExpenseRequest.Currency), $"Currency must be one of: {string.Join(", ", Currencies.Allowed)}.");

        if (date == default)
            AddError(nameof(CreateExpenseRequest.Date), "Date is required.");

        if (note?.Trim().Length > 1000)
            AddError(nameof(CreateExpenseRequest.Note), "Note must be 1000 characters or fewer.");

        return errors;
    }

    private static async Task<IResult> GetExpenses(
        ExpenseDbContext db,
        CancellationToken cancellationToken)
    {
        var expenses = await db.Expenses
            .AsNoTracking()
            .OrderByDescending(e => e.Date)
            .ThenByDescending(e => e.CreatedAt)
            .Select(e => new ExpenseResponse(e.Id, e.Amount, e.Currency, e.Description, e.Date, e.CreatedAt, e.Note))
            .ToListAsync(cancellationToken);

        return Results.Ok(expenses);
    }

    private static async Task<IResult> UpdateExpense(
        Guid id,
        UpdateExpenseRequest request,
        ExpenseDbContext db,
        CancellationToken cancellationToken)
    {
        // Validate before the lookup so a bad body is a 400 whether or not the id exists.
        var errors = ValidateExpense(request.Amount, request.Currency, request.Description, request.Date, request.Note);
        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        // Tracked read: EF then writes only the changed columns and leaves CreatedAt alone.
        var expense = await db.Expenses.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (expense is null)
        {
            return Results.NotFound();
        }

        request.ApplyTo(expense);
        await db.SaveChangesAsync(cancellationToken);

        return Results.Ok(expense.ToResponse());
    }

    private static async Task<IResult> DeleteExpense(
        Guid id,
        ExpenseDbContext db,
        CancellationToken cancellationToken)
    {
        var deleted = await db.Expenses
            .Where(e => e.Id == id)
            .ExecuteDeleteAsync(cancellationToken);

        return deleted > 0 ? Results.NoContent() : Results.NotFound();
    }

    private static async Task<IResult> GetMonthlyTotals(
        ExpenseDbContext db,
        CancellationToken cancellationToken,
        int? year = null,
        int? month = null)
    {
        var errors = new Dictionary<string, string[]>();
        void AddError(string field, string message) => errors[field] = [message];

        if (year is not null && month is null)
            AddError(nameof(month), "Month is required when year is supplied.");

        if (month is not null && year is null)
            AddError(nameof(year), "Year is required when month is supplied.");

        if (year is < 1 or > 9999)
            AddError(nameof(year), "Year must be between 1 and 9999.");

        if (month is < 1 or > 12)
            AddError(nameof(month), "Month must be between 1 and 12.");

        if (errors.Count == 0 && year is not null && month is not null)
        {
            var today = DateOnly.FromDateTime(DateTimeOffset.UtcNow.UtcDateTime);
            var requested = new DateOnly(year.Value, month.Value, 1);
            var currentMonth = new DateOnly(today.Year, today.Month, 1);

            if (requested > currentMonth)
                AddError(nameof(month), "Month cannot be in the future.");
        }

        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        var query = db.Expenses.AsQueryable();

        if (year is not null && month is not null)
        {
            query = query.Where(e => e.Date.Year == year.Value && e.Date.Month == month.Value);
        }

        var totals = await query
            .GroupBy(e => new { e.Date.Year, e.Date.Month, e.Currency })
            .OrderByDescending(g => g.Key.Year)
            .ThenByDescending(g => g.Key.Month)
            .ThenBy(g => g.Key.Currency)
            .Select(g => new MonthlyTotalResponse(
                g.Key.Year,
                g.Key.Month,
                g.Key.Currency,
                g.Sum(e => e.Amount)))
            .ToListAsync(cancellationToken);

        return Results.Ok(totals);
    }

    private static async Task<IResult> GetBudgets(
        ExpenseDbContext db,
        CancellationToken cancellationToken)
    {
        var budgets = await db.MonthlyBudgets
            .AsNoTracking()
            .OrderBy(b => b.Currency)
            .Select(b => new BudgetResponse(b.Id, b.Currency, b.Amount, b.UpdatedAt))
            .ToListAsync(cancellationToken);

        return Results.Ok(budgets);
    }

    private static async Task<IResult> SetBudget(
        SetBudgetRequest request,
        ExpenseDbContext db,
        CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();
        void AddError(string field, string message) => errors[field] = [message];

        if (request.Amount <= 0)
            AddError(nameof(request.Amount), "Amount must be greater than zero.");

        if (!Currencies.IsAllowed(request.Currency))
            AddError(nameof(request.Currency), $"Currency must be one of: {string.Join(", ", Currencies.Allowed)}.");

        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        var budget = await db.MonthlyBudgets
            .FirstOrDefaultAsync(b => b.Currency == request.Currency, cancellationToken);

        if (budget is null)
        {
            budget = new MonthlyBudget
            {
                Id = Guid.NewGuid(),
                Currency = request.Currency,
                Amount = request.Amount,
                UpdatedAt = DateTimeOffset.UtcNow,
            };
            db.MonthlyBudgets.Add(budget);

            try
            {
                await db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                // A concurrent request inserted the same currency first (unique index).
                // Re-read the winning row and update it instead.
                db.Entry(budget).State = EntityState.Detached;
                budget = await db.MonthlyBudgets
                    .FirstAsync(b => b.Currency == request.Currency, cancellationToken);
                budget.Amount = request.Amount;
                budget.UpdatedAt = DateTimeOffset.UtcNow;
                await db.SaveChangesAsync(cancellationToken);
            }
        }
        else
        {
            budget.Amount = request.Amount;
            budget.UpdatedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
        }

        return Results.Ok(budget.ToResponse());
    }

    private static async Task<IResult> GetBudgetComparison(
        ExpenseDbContext db,
        CancellationToken cancellationToken)
    {
        var totals = await db.Expenses
            .GroupBy(e => new { e.Date.Year, e.Date.Month, e.Currency })
            .OrderByDescending(g => g.Key.Year)
            .ThenByDescending(g => g.Key.Month)
            .ThenBy(g => g.Key.Currency)
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                g.Key.Currency,
                Spent = g.Sum(e => e.Amount),
            })
            .ToListAsync(cancellationToken);

        var budgets = await db.MonthlyBudgets
            .AsNoTracking()
            .ToDictionaryAsync(b => b.Currency, b => b.Amount, cancellationToken);

        var comparison = totals
            .Select(t =>
            {
                decimal? budget = budgets.TryGetValue(t.Currency, out var amount) ? amount : null;
                return new BudgetComparisonResponse(
                    t.Year,
                    t.Month,
                    t.Currency,
                    t.Spent,
                    budget,
                    budget - t.Spent);
            })
            .ToList();

        return Results.Ok(comparison);
    }
}
