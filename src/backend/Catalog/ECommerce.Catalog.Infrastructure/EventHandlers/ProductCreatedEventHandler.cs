using System.Threading;
using System.Threading.Tasks;
using ECommerce.Catalog.Application.Interfaces;
using ECommerce.Catalog.Domain.Aggregates.Product.Events;
using MediatR;

namespace ECommerce.Catalog.Infrastructure.EventHandlers;

public sealed class ProductCreatedEventHandler(IProductProjectionEventPublisher publisher)
    : INotificationHandler<ProductCreatedEvent>
{
    public Task Handle(ProductCreatedEvent notification, CancellationToken cancellationToken)
        => publisher.PublishProductProjectionUpdatedAsync(
            notification.ProductId,
            notification.Name,
            notification.Price,
            isDeleted: false,
            cancellationToken);
}
