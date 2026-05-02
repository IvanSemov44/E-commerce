using ECommerce.Contracts;

namespace ECommerce.Ordering.Infrastructure.Integration;

public interface IOrderingOutboxEventWriter
{
    Task EnqueueAsync(IntegrationEvent integrationEvent, CancellationToken cancellationToken = default);
}
