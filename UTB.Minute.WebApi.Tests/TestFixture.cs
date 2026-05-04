using Aspire.Hosting;
using Aspire.Hosting.Testing;
using Microsoft.EntityFrameworkCore;
using UTB.Minute.Db;
using UTB.Minute.Db.Entities;
using Xunit;

namespace UTB.Minute.WebApi.Tests;

/// <summary>
/// Test fixture for integration tests using Aspire and PostgreSQL
/// Provides database context and HTTP client for testing
/// </summary>
public class TestFixture : IAsyncLifetime
{
    private DistributedApplication app = null!;
    private string? connectionString;
    public HttpClient HttpClient { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        var builder = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.UTB_Minute_AppHost>(["--environment=Testing"], CancellationToken.None);

        app = await builder.BuildAsync(CancellationToken.None);

        await app.StartAsync(CancellationToken.None);

        await app.ResourceNotifications.WaitForResourceHealthyAsync("database", CancellationToken.None);
        await app.ResourceNotifications.WaitForResourceHealthyAsync("webapi", CancellationToken.None);

        connectionString = await app.GetConnectionStringAsync("database", CancellationToken.None);
        HttpClient = app.CreateHttpClient("webapi", "https");

        using var context = CreateContext();

        await context.Database.EnsureDeletedAsync(CancellationToken.None);
        await context.Database.EnsureCreatedAsync(CancellationToken.None);

        // Seed test data
        context.Meals.AddRange(
            new Meal { Name = "Guláš", Description = "Tradiční hovězí guláš", Price = 120, IsActive = true },
            new Meal { Name = "Svíčková", Description = "Hovězí svíčková se smetanou", Price = 150, IsActive = true },
            new Meal { Name = "Rizoto", Description = "Houbové rizoto", Price = 140, IsActive = false }
        );

        await context.SaveChangesAsync(CancellationToken.None);
    }

    public MinuteDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<MinuteDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new MinuteDbContext(options);
    }

    /// <summary>
    /// Resets the database to its initial state with seed data
    /// Call this method in test setup to ensure clean state
    /// </summary>
    public async Task ResetDatabaseAsync()
    {
        using var context = CreateContext();

        // Delete all data in correct order (respecting foreign keys)
        await context.Orders.ExecuteDeleteAsync(CancellationToken.None);
        await context.MenuItems.ExecuteDeleteAsync(CancellationToken.None);
        await context.Meals.ExecuteDeleteAsync(CancellationToken.None);

        // Re-seed Meals
        var meals = new List<Meal>
        {
            new Meal { Name = "Guláš", Description = "Tradiční hovězí guláš", Price = 120, IsActive = true },
            new Meal { Name = "Svíčková", Description = "Hovězí svíčková se smetanou", Price = 150, IsActive = true },
            new Meal { Name = "Rizoto", Description = "Houbové rizoto", Price = 140, IsActive = false }
        };
        context.Meals.AddRange(meals);
        await context.SaveChangesAsync(CancellationToken.None);

        // Re-seed MenuItems
        var today = DateOnly.FromDateTime(DateTime.Today);
        var tomorrow = today.AddDays(1);

        var menuItems = new List<MenuItem>
        {
            new MenuItem { Date = today, MealId = meals[0].Id, AvailablePortions = 10 },
            new MenuItem { Date = today, MealId = meals[1].Id, AvailablePortions = 15 },
            new MenuItem { Date = tomorrow, MealId = meals[0].Id, AvailablePortions = 8 }
        };
        context.MenuItems.AddRange(menuItems);
        await context.SaveChangesAsync(CancellationToken.None);
    }

    public async Task DisposeAsync()
    {
        HttpClient.Dispose();
        await app.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}
