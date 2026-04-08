using ECommerce.Infrastructure.Data;
using ECommerce.Infrastructure.Data.Configurations;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Integration;

/// <summary>
/// Dedicated persistence boundary for integration reliability patterns.
/// </summary>
public sealed class IntegrationPersistenceDbContext(DbContextOptions<IntegrationPersistenceDbContext> options)
    : DbContext(options)
{
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<DeadLetterMessage> DeadLetterMessages => Set<DeadLetterMessage>();
    public DbSet<OrderFulfillmentSagaState> OrderFulfillmentSagaStates => Set<OrderFulfillmentSagaState>();
    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration());
        modelBuilder.ApplyConfiguration(new DeadLetterMessageConfiguration());
        modelBuilder.ApplyConfiguration(new OrderFulfillmentSagaStateConfiguration());
        modelBuilder.ApplyConfiguration(new InboxMessageConfiguration());
    }
}
