using ECommerce.Reviews.Domain.Aggregates.Review;
using ECommerce.SharedKernel.Domain;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Reviews.Infrastructure.Persistence;

public class ReviewsDbContext(
    DbContextOptions<ReviewsDbContext> options,
    IDomainEventDispatcher? dispatcher = null) : DbContext(options)
{
    private readonly IDomainEventDispatcher? _dispatcher = dispatcher;

    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<ProductReadModel> Products => Set<ProductReadModel>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();
    public DbSet<DeadLetterMessage> DeadLetterMessages => Set<DeadLetterMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ReviewsDbContext).Assembly);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var aggregates = ChangeTracker.Entries<AggregateRoot>()
            .Where(e => e.Entity.DomainEvents.Count != 0)
            .Select(e => e.Entity)
            .ToList();

        var events = aggregates.SelectMany(a => a.DomainEvents).ToList();
        foreach (var aggregate in aggregates)
            aggregate.ClearDomainEvents();

        // Save aggregate changes FIRST so event handlers (e.g. rating publisher) can
        // query the updated state (e.g. recalculate average after the new review is saved).
        int result = await base.SaveChangesAsync(cancellationToken);

        // Dispatch AFTER aggregate save. Handlers add outbox rows to this DbContext.
        if (_dispatcher is not null && events.Count != 0)
            await _dispatcher.DispatchEventsAsync(events, cancellationToken);

        // Second save: persist outbox rows added by event handlers.
        // Two commits on one connection — not fully atomic, but contained within Reviews schema.
        if (ChangeTracker.HasChanges())
            await base.SaveChangesAsync(cancellationToken);

        return result;
    }
}
