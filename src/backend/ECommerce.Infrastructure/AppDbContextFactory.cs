using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using ECommerce.Infrastructure.Data;

namespace ECommerce.Infrastructure;

/// <summary>
/// Design-time factory for creating AppDbContext instances.
/// Used by EF Core CLI tools (migrations, database update, etc.)
/// </summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        
        // Get connection string from environment variable or use default for local development
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Host=localhost;Database=ECommerceDb;Username=ecommerce;Password=local-dev-password-123";
        
        optionsBuilder.UseNpgsql(connectionString);
        
        return new AppDbContext(optionsBuilder.Options);
    }
}
