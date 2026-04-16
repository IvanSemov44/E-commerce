using System.Threading;
using System.Threading.Tasks;
using ECommerce.Catalog.Application.Interfaces;
using ECommerce.Catalog.Domain.Aggregates.Product.Events;
using MediatR;

namespace ECommerce.Catalog.Infrastructure.EventHandlers;

public sealed class ProductImageAddedEventHandler(IProductProjectionEventPublisher publisher)
    : INotificationHandler<ProductImageAddedEvent>
{
    public Task Handle(ProductImageAddedEvent notification, CancellationToken cancellationToken)
        => publisher.PublishProductImageProjectionUpdatedAsync(
            notification.ImageId,
            notification.ProductId,
            notification.Url,
            notification.IsPrimary,
            isDeleted: false,
            cancellationToken);
}
