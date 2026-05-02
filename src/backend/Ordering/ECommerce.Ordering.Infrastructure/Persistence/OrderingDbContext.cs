using ECommerce.Ordering.Domain.Aggregates.Order;
using ECommerce.SharedKernel.Domain;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Ordering.Infrastructure.Persistence;

public class OrderingDbContext(
    DbContextOptions<OrderingDbContext> options,
    IDomainEventDispatcher? dispatcher = null) : DbContext(options)
{
    private readonly IDomainEventDispatcher? _dispatcher = dispatcher;

    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<ProductReadModel> Products => Set<ProductReadModel>();
    public DbSet<ProductImageReadModel> ProductImages => Set<ProductImageReadModel>();
    public DbSet<PromoCodeReadModel> PromoCodes => Set<PromoCodeReadModel>();
    public DbSet<AddressReadModel> Addresses => Set<AddressReadModel>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();
    public DbSet<DeadLetterMessage> DeadLetterMessages => Set<DeadLetterMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema("ordering");

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OrderingDbContext).Assembly);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var utcNow = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<Entity>().Where(e => e.State == EntityState.Modified))
            entry.Property(nameof(Entity.UpdatedAt)).CurrentValue = utcNow;

        var aggregates = ChangeTracker.Entries<AggregateRoot>()
            .Where(e => e.Entity.DomainEvents.Count != 0)
            .Select(e => e.Entity)
            .ToList();

        var events = aggregates.SelectMany(a => a.DomainEvents).ToList();
        foreach (var aggregate in aggregates)
            aggregate.ClearDomainEvents();

        if (_dispatcher is not null && events.Count != 0)
            await _dispatcher.DispatchEventsAsync(events, cancellationToken);

        return await base.SaveChangesAsync(cancellationToken);
    }
}
