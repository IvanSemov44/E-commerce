using ECommerce.Promotions.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ECommerce.Promotions.Infrastructure;

/// <summary>
/// Design-time factory used by EF Core CLI tools (migrations, database update).
/// </summary>
public sealed class PromotionsDbContextFactory : IDesignTimeDbContextFactory<PromotionsDbContext>
{
    public PromotionsDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__PromotionsConnection")
            ?? "Host=localhost;Database=ECommerceDb;Username=ecommerce;Password=local-dev-password-123";

        var options = new DbContextOptionsBuilder<PromotionsDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new PromotionsDbContext(options);
    }
}
