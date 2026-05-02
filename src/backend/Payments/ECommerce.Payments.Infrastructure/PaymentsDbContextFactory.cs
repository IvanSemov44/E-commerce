using ECommerce.Payments.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ECommerce.Payments.Infrastructure;

/// <summary>
/// Design-time factory used by EF Core CLI tools (migrations, database update).
/// </summary>
public sealed class PaymentsDbContextFactory : IDesignTimeDbContextFactory<PaymentsDbContext>
{
    public PaymentsDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__PaymentsConnection")
            ?? "Host=localhost;Database=ECommerceDb;Username=ecommerce;Password=local-dev-password-123";

        var options = new DbContextOptionsBuilder<PaymentsDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new PaymentsDbContext(options);
    }
}
