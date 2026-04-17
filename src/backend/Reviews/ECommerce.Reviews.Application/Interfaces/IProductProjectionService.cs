namespace ECommerce.Reviews.Application.Interfaces;

public interface IProductProjectionService
{
    Task<bool> ProductExistsAsync(Guid productId, CancellationToken cancellationToken = default);
}
