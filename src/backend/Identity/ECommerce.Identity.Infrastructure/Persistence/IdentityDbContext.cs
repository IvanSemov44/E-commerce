using ECommerce.Identity.Domain.Aggregates.User;
using ECommerce.Identity.Infrastructure.Persistence.Configurations;
using ECommerce.SharedKernel.Domain;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Identity.Infrastructure.Persistence;

public class IdentityDbContext(DbContextOptions<IdentityDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Address> Addresses => Set<Address>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema("identity");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(UserAggregateConfiguration).Assembly);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var utcNow = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<Entity>())
        {
            if (entry.State == EntityState.Modified)
                entry.Property(nameof(Entity.UpdatedAt)).CurrentValue = utcNow;

            if (entry.State == EntityState.Added)
            {
                entry.Property(nameof(Entity.CreatedAt)).CurrentValue = utcNow;
                entry.Property(nameof(Entity.UpdatedAt)).CurrentValue = utcNow;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
