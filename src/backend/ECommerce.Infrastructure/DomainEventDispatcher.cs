using ECommerce.SharedKernel.Domain;
using MediatR;

namespace ECommerce.Infrastructure;

public class DomainEventDispatcher(IMediator mediator) : IDomainEventDispatcher
{
    public async Task DispatchEventsAsync(IEnumerable<IDomainEvent> events, CancellationToken cancellationToken = default)
    {
        foreach (var domainEvent in events)
            await mediator.Publish(domainEvent, cancellationToken);
    }
}
