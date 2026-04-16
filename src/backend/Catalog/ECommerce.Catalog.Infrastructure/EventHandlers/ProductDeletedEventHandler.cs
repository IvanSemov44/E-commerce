using System.Threading;
using System.Threading.Tasks;
using ECommerce.Catalog.Application.Interfaces;
using ECommerce.Catalog.Domain.Aggregates.Product.Events;
using MediatR;

namespace ECommerce.Catalog.Infrastructure.EventHandlers;

public sealed class ProductDeletedEventHandler(IProductProjectionEventPublisher publisher)
    : INotificationHandler<ProductDeletedEvent>
{
    public async Task Handle(ProductDeletedEvent notification, CancellationToken cancellationToken)
    {
        await publisher.PublishProductProjectionUpdatedAsync(
            notification.ProductId,
            notification.Name,
            notification.Price,
            isDeleted: true,
            cancellationToken);

        foreach (var image in notification.Images)
        {
            await publisher.PublishProductImageProjectionUpdatedAsync(
                image.ImageId,
                notification.ProductId,
                image.Url,
                image.IsPrimary,
                isDeleted: true,
                cancellationToken);
        }
    }
}
