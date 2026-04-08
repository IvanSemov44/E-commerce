using ECommerce.Infrastructure.Data;
using ECommerce.Infrastructure.Data.Seeders;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ECommerce.Infrastructure;

public interface IAppDbInitializationService
{
    Task InitializeAsync(IHostEnvironment environment, CancellationToken cancellationToken = default);
}

public sealed class AppDbInitializationService(
    AppDbContext context,
    DatabaseSeeder seeder,
    ILogger<AppDbInitializationService> logger) : IAppDbInitializationService
{
    public async Task InitializeAsync(IHostEnvironment environment, CancellationToken cancellationToken = default)
    {
        await ApplyMigrationsAsync(cancellationToken);

        // Integration tests seed deterministic data in TestWebApplicationFactory.
        if (!environment.IsEnvironment("Test"))
            await SeedDatabaseAsync(environment);
    }

    private async Task ApplyMigrationsAsync(CancellationToken cancellationToken)
    {
        var providerName = context.Database.ProviderName ?? string.Empty;
        if (providerName.Contains("InMemory", StringComparison.OrdinalIgnoreCase) || !context.Database.IsRelational())
        {
            logger.LogInformation("Skipping migration for non-relational provider: {Provider}", providerName);
            return;
        }

        IEnumerable<string> pendingMigrations = await GetPendingMigrationsSafeAsync(cancellationToken);

        if (!pendingMigrations.Any())
        {
            logger.LogInformation("No pending migrations found.");
            return;
        }

        logger.LogInformation("Applying pending migrations... Count: {Count}", pendingMigrations.Count());
        foreach (string migration in pendingMigrations)
            logger.LogInformation("Pending migration: {Migration}", migration);

        await context.Database.MigrateAsync(cancellationToken);
        logger.LogInformation("Migrations applied successfully.");
    }

    private async Task<IEnumerable<string>> GetPendingMigrationsSafeAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await Task.Run(() => context.Database.GetPendingMigrations(), cancellationToken);
        }
        catch (InvalidOperationException)
        {
            return Enumerable.Empty<string>();
        }
        catch (Exception ex) when (ex.Message.Contains("__EFMigrationsHistory") ||
                                   ex.InnerException?.Message.Contains("__EFMigrationsHistory") == true)
        {
            logger.LogInformation("Migration history table does not exist, all migrations will be applied.");
            return await Task.Run(() => context.Database.GetMigrations(), cancellationToken);
        }
    }

    private async Task SeedDatabaseAsync(IHostEnvironment environment)
    {
        try
        {
            logger.LogInformation("Seeding database with sample data...");
            await seeder.SeedAsync(context, environment);
            logger.LogInformation("Database seeding completed.");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "An error occurred while seeding database.");
        }
    }
}
