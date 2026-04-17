using ECommerce.Reviews.Application.Interfaces;
using ECommerce.Reviews.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Reviews.Infrastructure.Services;

public class ProductProjectionService(ReviewsDbContext reviewsDbContext) : IProductProjectionService
{
    public Task<bool> ProductExistsAsync(Guid productId, CancellationToken cancellationToken = default)
        => reviewsDbContext.Products
            .AsNoTracking()
            .AnyAsync(product => product.Id == productId && product.IsActive, cancellationToken);
}
