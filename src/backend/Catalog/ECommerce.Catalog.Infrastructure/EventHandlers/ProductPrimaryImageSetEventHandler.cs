using System.Threading;
using System.Threading.Tasks;
using ECommerce.Catalog.Application.Interfaces;
using ECommerce.Catalog.Domain.Aggregates.Product.Events;
using MediatR;

namespace ECommerce.Catalog.Infrastructure.EventHandlers;

public sealed class ProductPrimaryImageSetEventHandler(IProductProjectionEventPublisher publisher)
    : INotificationHandler<ProductPrimaryImageSetEvent>
{
    public async Task Handle(ProductPrimaryImageSetEvent notification, CancellationToken cancellationToken)
    {
        // Demote the old primary — only if it was a different image
        if (notification.OldPrimaryImageId.HasValue &&
            notification.OldPrimaryImageId != notification.NewPrimaryImageId)
        {
            await publisher.PublishProductImageProjectionUpdatedAsync(
                notification.OldPrimaryImageId.Value,
                notification.ProductId,
                notification.OldPrimaryImageUrl!,
                isPrimary: false,
                isDeleted: false,
                cancellationToken);
        }

        // Promote the new primary
        await publisher.PublishProductImageProjectionUpdatedAsync(
            notification.NewPrimaryImageId,
            notification.ProductId,
            notification.NewPrimaryImageUrl,
            isPrimary: true,
            isDeleted: false,
            cancellationToken);
    }
}
