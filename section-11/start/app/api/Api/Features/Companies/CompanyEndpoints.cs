using ExpenseTracker.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Api.Features.Companies;

public static class CompanyEndpoints
{
    public static IEndpointRouteBuilder MapCompanyEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/companies");

        group.MapPost("/", CreateCompany);
        group.MapGet("/", GetCompanies);
        group.MapDelete("/{id:guid}", DeleteCompany);

        return app;
    }

    private static async Task<IResult> CreateCompany(
        CreateCompanyRequest request,
        ExpenseDbContext db,
        CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();
        void AddError(string field, string message) => errors[field] = [message];

        var name = request.Name?.Trim();
        if (string.IsNullOrWhiteSpace(name))
            AddError(nameof(request.Name), "Name is required.");
        else if (name.Length > 200)
            AddError(nameof(request.Name), "Name must be 200 characters or fewer.");

        var website = request.Website?.Trim();
        if (string.IsNullOrWhiteSpace(website))
            AddError(nameof(request.Website), "Website is required.");
        else if (website.Length > 200)
            AddError(nameof(request.Website), "Website must be 200 characters or fewer.");

        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        var company = request.ToEntity();
        db.Companies.Add(company);
        await db.SaveChangesAsync(cancellationToken);

        return Results.Created($"/api/companies/{company.Id}", company.ToResponse());
    }

    private static async Task<IResult> GetCompanies(
        ExpenseDbContext db,
        CancellationToken cancellationToken)
    {
        var companies = await db.Companies
            .AsNoTracking()
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new CompanyResponse(c.Id, c.Name, c.Website, c.CreatedAt))
            .ToListAsync(cancellationToken);

        return Results.Ok(companies);
    }

    private static async Task<IResult> DeleteCompany(
        Guid id,
        ExpenseDbContext db,
        CancellationToken cancellationToken)
    {
        var deleted = await db.Companies
            .Where(c => c.Id == id)
            .ExecuteDeleteAsync(cancellationToken);

        return deleted > 0 ? Results.NoContent() : Results.NotFound();
    }
}
