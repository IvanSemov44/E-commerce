using System.Threading;
using System.Threading.Tasks;
using ECommerce.Catalog.Application.Interfaces;
using ECommerce.Catalog.Domain.Aggregates.Product.Events;
using MediatR;

namespace ECommerce.Catalog.Infrastructure.EventHandlers;

public sealed class ProductPriceChangedEventHandler(IProductProjectionEventPublisher publisher)
    : INotificationHandler<ProductPriceChangedEvent>
{
    public Task Handle(ProductPriceChangedEvent notification, CancellationToken cancellationToken)
        => publisher.PublishProductProjectionUpdatedAsync(
            notification.ProductId,
            notification.Name,
            notification.NewPrice.Amount,
            isDeleted: false,
            cancellationToken);
}
