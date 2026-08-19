namespace ExpenseTracker.Api.Features.Companies;

public record CreateCompanyRequest(
    string Name,
    string Website);

public record CompanyResponse(
    Guid Id,
    string Name,
    string Website,
    DateTimeOffset CreatedAt);

/// <summary>
/// Explicit mapping between the entity and its DTOs. No AutoMapper by design.
/// </summary>
public static class CompanyMapping
{
    public static Company ToEntity(this CreateCompanyRequest request) => new()
    {
        Id = Guid.NewGuid(),
        Name = request.Name.Trim(),
        Website = request.Website.Trim(),
    };

    public static CompanyResponse ToResponse(this Company company) => new(
        company.Id,
        company.Name,
        company.Website,
        company.CreatedAt);
}
