namespace ECommerce.Reviews.Application.Interfaces;

public interface ICatalogService
{
    Task<bool> ProductExistsAsync(Guid productId, CancellationToken cancellationToken = default);
}
