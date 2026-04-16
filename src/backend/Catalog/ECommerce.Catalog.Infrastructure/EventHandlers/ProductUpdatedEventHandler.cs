using System.Threading;
using System.Threading.Tasks;
using ECommerce.Catalog.Application.Interfaces;
using ECommerce.Catalog.Domain.Aggregates.Product.Events;
using MediatR;

namespace ECommerce.Catalog.Infrastructure.EventHandlers;

public sealed class ProductUpdatedEventHandler(IProductProjectionEventPublisher publisher)
    : INotificationHandler<ProductUpdatedEvent>
{
    public Task Handle(ProductUpdatedEvent notification, CancellationToken cancellationToken)
        => publisher.PublishProductProjectionUpdatedAsync(
            notification.ProductId,
            notification.Name,
            notification.Price,
            isDeleted: false,
            cancellationToken);
}
