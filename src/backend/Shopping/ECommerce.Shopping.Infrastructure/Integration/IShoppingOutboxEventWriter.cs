using ECommerce.Contracts;

namespace ECommerce.Shopping.Infrastructure.Integration;

public interface IShoppingOutboxEventWriter
{
    Task EnqueueAsync(IntegrationEvent integrationEvent, CancellationToken cancellationToken = default);
}
