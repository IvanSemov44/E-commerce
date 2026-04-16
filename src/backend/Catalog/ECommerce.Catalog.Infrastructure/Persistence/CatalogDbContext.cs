using ECommerce.Catalog.Domain.Aggregates.Category;
using ECommerce.Catalog.Domain.Aggregates.Product;
using ECommerce.Infrastructure.Data;
using ECommerce.SharedKernel.Domain;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Catalog.Infrastructure.Persistence;

public class CatalogDbContext(DbContextOptions<CatalogDbContext> options, IDomainEventDispatcher? dispatcher = null) : DbContext(options)
{
    private readonly IDomainEventDispatcher? _dispatcher = dispatcher;

    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<ProductRatingReadModel> ProductRatings => Set<ProductRatingReadModel>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();
    public DbSet<DeadLetterMessage> DeadLetterMessages => Set<DeadLetterMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("catalog");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CatalogDbContext).Assembly);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Collect and clear domain events before dispatching
        var aggregates = ChangeTracker.Entries<AggregateRoot>()
            .Where(e => e.Entity.DomainEvents.Count != 0)
            .Select(e => e.Entity)
            .ToList();

        var events = aggregates.SelectMany(a => a.DomainEvents).ToList();
        foreach (var aggregate in aggregates)
            aggregate.ClearDomainEvents();

        // Dispatch BEFORE saving so event handlers can enqueue outbox rows
        // into this same DbContext — all committed in the single save below.
        if (_dispatcher is not null && events.Count != 0)
            await _dispatcher.DispatchEventsAsync(events, cancellationToken);

        // One save: aggregate changes + outbox rows — atomic on a single connection.
        return await base.SaveChangesAsync(cancellationToken);
    }
}
