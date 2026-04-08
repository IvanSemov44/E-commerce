using System;
using System.Threading;
using System.Threading.Tasks;

namespace ECommerce.Catalog.Application.Interfaces;

public interface IProductProjectionEventPublisher
{
    Task PublishProductProjectionUpdatedAsync(
        Guid productId,
        string name,
        decimal price,
        bool isDeleted,
        CancellationToken cancellationToken = default);

    Task PublishProductImageProjectionUpdatedAsync(
        Guid imageId,
        Guid productId,
        string url,
        bool isPrimary,
        bool isDeleted,
        CancellationToken cancellationToken = default);
}
