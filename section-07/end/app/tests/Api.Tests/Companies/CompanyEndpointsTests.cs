using System.Net;
using System.Net.Http.Json;
using ExpenseTracker.Api.Data;
using ExpenseTracker.Api.Features.Companies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ExpenseTracker.Api.Tests.Companies;

public class CompanyEndpointsTests(ExpenseApiFactory factory) : IAsyncLifetime
{
    private readonly ExpenseApiFactory _factory = factory;

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public ValueTask InitializeAsync() => new(_factory.ResetAsync());

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private static CreateCompanyRequest ValidRequest(
        string name = "Acme Corp",
        string website = "https://acme.example.com") =>
        new(name, website);

    [Fact]
    public async Task Post_Should_Return_201_And_Persist_When_Valid()
    {
        var client = _factory.CreateClient();
        var request = ValidRequest(name: "Globex", website: "https://globex.example.com");

        var response = await client.PostAsJsonAsync("/api/companies", request, Ct);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        var created = await response.Content.ReadFromJsonAsync<CompanyResponse>(Ct);
        Assert.NotNull(created);
        Assert.NotEqual(Guid.Empty, created!.Id);
        Assert.Equal(request.Name, created.Name);
        Assert.Equal(request.Website, created.Website);
        Assert.NotEqual(default, created.CreatedAt);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ExpenseDbContext>();
        var persisted = await db.Companies.SingleAsync(Ct);
        Assert.Equal(created.Id, persisted.Id);
        Assert.Equal("Globex", persisted.Name);
    }

    [Fact]
    public async Task Get_Should_Return_Companies_Ordered_By_CreatedAt_Desc()
    {
        var client = _factory.CreateClient();
        await client.PostAsJsonAsync("/api/companies", ValidRequest(name: "First"), Ct);
        await client.PostAsJsonAsync("/api/companies", ValidRequest(name: "Second"), Ct);
        await client.PostAsJsonAsync("/api/companies", ValidRequest(name: "Third"), Ct);

        var companies = await client.GetFromJsonAsync<List<CompanyResponse>>("/api/companies", Ct);

        Assert.NotNull(companies);
        Assert.Equal(3, companies!.Count);
        Assert.Collection(companies,
            c => Assert.Equal("Third", c.Name),
            c => Assert.Equal("Second", c.Name),
            c => Assert.Equal("First", c.Name));
    }

    [Fact]
    public async Task Delete_Should_Remove_Row_And_Second_Delete_Should_Return_404()
    {
        var client = _factory.CreateClient();
        var post = await client.PostAsJsonAsync("/api/companies", ValidRequest(), Ct);
        var created = await post.Content.ReadFromJsonAsync<CompanyResponse>(Ct);
        Assert.NotNull(created);

        var first = await client.DeleteAsync($"/api/companies/{created!.Id}", Ct);
        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);

        var second = await client.DeleteAsync($"/api/companies/{created.Id}", Ct);
        Assert.Equal(HttpStatusCode.NotFound, second.StatusCode);

        var remaining = await client.GetFromJsonAsync<List<CompanyResponse>>("/api/companies", Ct);
        Assert.Empty(remaining!);
    }

    [Fact]
    public async Task Post_Should_Return_400_When_Name_Empty()
    {
        var client = _factory.CreateClient();
        var request = ValidRequest(name: "   ");

        var response = await client.PostAsJsonAsync("/api/companies", request, Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblem>(Ct);
        Assert.NotNull(problem);
        Assert.Contains("Name", problem!.Errors.Keys);
    }

    [Fact]
    public async Task Post_Should_Return_400_When_Website_Empty()
    {
        var client = _factory.CreateClient();
        var request = ValidRequest(website: "   ");

        var response = await client.PostAsJsonAsync("/api/companies", request, Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblem>(Ct);
        Assert.NotNull(problem);
        Assert.Contains("Website", problem!.Errors.Keys);
    }

    private sealed record ValidationProblem(IDictionary<string, string[]> Errors);
}
