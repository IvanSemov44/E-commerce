using ECommerce.Catalog.Application.Interfaces;
using ECommerce.Contracts;
using MediatR;

namespace ECommerce.Catalog.Infrastructure.Services;

public sealed class ProductProjectionEventPublisher(IPublisher publisher) : IProductProjectionEventPublisher
{
    public Task PublishProductProjectionUpdatedAsync(
        Guid productId,
        string name,
        decimal price,
        bool isDeleted,
        CancellationToken cancellationToken = default)
    {
        var integrationEvent = new ProductProjectionUpdatedIntegrationEvent(
            productId,
            name,
            price,
            isDeleted,
            DateTime.UtcNow);

        return publisher.Publish(integrationEvent, cancellationToken);
    }

    public Task PublishProductImageProjectionUpdatedAsync(
        Guid imageId,
        Guid productId,
        string url,
        bool isPrimary,
        bool isDeleted,
        CancellationToken cancellationToken = default)
    {
        var integrationEvent = new ProductImageProjectionUpdatedIntegrationEvent(
            imageId,
            productId,
            url,
            isPrimary,
            isDeleted,
            DateTime.UtcNow);

        return publisher.Publish(integrationEvent, cancellationToken);
    }
}
