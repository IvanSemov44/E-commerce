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

        var events = aggregates
            .SelectMany(a => a.DomainEvents)
            .ToList();

        foreach (var aggregate in aggregates)
            aggregate.ClearDomainEvents();

        int result = await base.SaveChangesAsync(cancellationToken);

        if (_dispatcher != null && events.Count != 0)
            await _dispatcher.DispatchEventsAsync(events, cancellationToken);

        return result;
    }
}
