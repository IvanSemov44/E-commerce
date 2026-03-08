using ECommerce.Core.Interfaces.Repositories;
using ECommerce.Infrastructure;
using ECommerce.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Tests.Helpers;

/// <summary>
/// Base class for integration tests that need a database context.
/// Uses EF Core InMemory database for testing.
/// </summary>
public abstract class IntegrationTestBase : IDisposable
{
    protected AppDbContext DbContext { get; }
    protected IUnitOfWork UnitOfWork { get; }

    protected IntegrationTestBase()
    {
        // Create a unique in-memory database for each test
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        DbContext = new AppDbContext(options);
        UnitOfWork = new UnitOfWork(DbContext);

        // Ensure database is created
        DbContext.Database.EnsureCreated();
    }

    /// <summary>
    /// Seeds the database with test data.
    /// Override this method in derived classes to add custom seed data.
    /// </summary>
    protected virtual async Task SeedDatabaseAsync()
    {
        await Task.CompletedTask;
    }

    /// <summary>
    /// Clears all data from the database.
    /// </summary>
    protected async Task ClearDatabaseAsync()
    {
        DbContext.Users.RemoveRange(DbContext.Users);
        DbContext.Products.RemoveRange(DbContext.Products);
        DbContext.Categories.RemoveRange(DbContext.Categories);
        DbContext.Orders.RemoveRange(DbContext.Orders);
        DbContext.OrderItems.RemoveRange(DbContext.OrderItems);
        DbContext.Carts.RemoveRange(DbContext.Carts);
        DbContext.CartItems.RemoveRange(DbContext.CartItems);
        DbContext.PromoCodes.RemoveRange(DbContext.PromoCodes);
        DbContext.Reviews.RemoveRange(DbContext.Reviews);
        DbContext.Wishlists.RemoveRange(DbContext.Wishlists);
        DbContext.InventoryLogs.RemoveRange(DbContext.InventoryLogs);
        DbContext.Addresses.RemoveRange(DbContext.Addresses);

        await DbContext.SaveChangesAsync();
    }

    public void Dispose()
    {
        DbContext.Dispose();
        GC.SuppressFinalize(this);
    }
}
