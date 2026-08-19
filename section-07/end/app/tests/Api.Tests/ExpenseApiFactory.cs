using ExpenseTracker.Api;
using ExpenseTracker.Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace ExpenseTracker.Api.Tests;

/// <summary>
/// Boots the API against a throwaway Postgres container and applies EF Core migrations
/// before any test runs. Registered once for the whole assembly (see AssemblyFixtures.cs);
/// each test resets the tables so no state leaks between tests.
/// </summary>
public sealed class ExpenseApiFactory : WebApplicationFactory<IApiAssemblyMarker>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:18")
        .Build();

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ExpenseDbContext>();
        await db.Database.MigrateAsync();
    }

    /// <summary>Removes all rows so each test starts from a clean table.</summary>
    public async Task ResetAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ExpenseDbContext>();
        await db.Expenses.ExecuteDeleteAsync();
        await db.MonthlyBudgets.ExecuteDeleteAsync();
        await db.Subscriptions.ExecuteDeleteAsync();
        await db.Companies.ExecuteDeleteAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Postgres"] = _container.GetConnectionString(),
            });
        });
    }

    public override async ValueTask DisposeAsync()
    {
        await _container.DisposeAsync();
        await base.DisposeAsync();
    }
}
