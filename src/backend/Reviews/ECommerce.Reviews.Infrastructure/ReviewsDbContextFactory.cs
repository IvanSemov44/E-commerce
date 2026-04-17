using ECommerce.Reviews.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ECommerce.Reviews.Infrastructure;

/// <summary>
/// Design-time factory used exclusively by the EF Core tooling (dotnet ef migrations).
/// Not used at runtime — the DI container builds ReviewsDbContext via AddDbContext.
/// </summary>
public sealed class ReviewsDbContextFactory : IDesignTimeDbContextFactory<ReviewsDbContext>
{
    public ReviewsDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__ReviewsConnection")
            ?? "Host=localhost;Database=ECommerceDb;Username=ecommerce;Password=local-dev-password-123";

        var options = new DbContextOptionsBuilder<ReviewsDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new ReviewsDbContext(options);
    }
}
