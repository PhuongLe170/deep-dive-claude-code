using ExpenseTracker.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Api.Features.Expenses;

public static class ExpenseEndpoints
{
    public static IEndpointRouteBuilder MapExpenseEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/expenses");

        group.MapPost("/", CreateExpense);
        group.MapGet("/", GetExpenses);
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
        var errors = new Dictionary<string, string[]>();
        void AddError(string field, string message) => errors[field] = [message];

        if (request.Amount <= 0)
            AddError(nameof(request.Amount), "Amount must be greater than zero.");

        var description = request.Description?.Trim();
        if (string.IsNullOrWhiteSpace(description))
            AddError(nameof(request.Description), "Description is required.");
        else if (description.Length > 200)
            AddError(nameof(request.Description), "Description must be 200 characters or fewer.");

        if (!Currencies.IsAllowed(request.Currency))
            AddError(nameof(request.Currency), $"Currency must be one of: {string.Join(", ", Currencies.Allowed)}.");

        if (request.Date == default)
            AddError(nameof(request.Date), "Date is required.");

        if (request.Note?.Trim().Length > 1000)
            AddError(nameof(request.Note), "Note must be 1000 characters or fewer.");

        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        var expense = request.ToEntity();
        db.Expenses.Add(expense);
        await db.SaveChangesAsync(cancellationToken);

        return Results.Created($"/api/expenses/{expense.Id}", expense.ToResponse());
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
        CancellationToken cancellationToken)
    {
        var totals = await db.Expenses
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
