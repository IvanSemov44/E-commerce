using ECommerce.Contracts;

namespace ECommerce.Payments.Infrastructure.Integration;

public interface IPaymentsOutboxEventWriter
{
    Task EnqueueAsync(IntegrationEvent integrationEvent, CancellationToken cancellationToken = default);
}
