using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using ECommerce.SharedKernel.Domain;
using ECommerce.Infrastructure.Data;

namespace ECommerce.Infrastructure;

public class DomainEventDispatcher(AppDbContext context, IMediator mediator) : IDomainEventDispatcher
{
    private readonly AppDbContext _context = context;
    private readonly IMediator _mediator = mediator;

    public async Task DispatchEventsAsync(CancellationToken cancellationToken = default)
    {
        var domainEntities = _context.ChangeTracker
            .Entries<AggregateRoot>()
            .Where(e => e.Entity.DomainEvents != null && e.Entity.DomainEvents.Count != 0)
            .ToList();

        var domainEvents = domainEntities
            .SelectMany(e => e.Entity.DomainEvents)
            .ToList();

        // Clear domain events on each aggregate before publishing
        foreach (var entry in domainEntities)
        {
            entry.Entity.ClearDomainEvents();
        }

        // Publish events
        foreach (var domainEvent in domainEvents)
        {
            await _mediator.Publish(domainEvent, cancellationToken);
        }
    }
}
