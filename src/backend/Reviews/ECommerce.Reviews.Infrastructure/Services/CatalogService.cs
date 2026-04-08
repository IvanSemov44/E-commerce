using ECommerce.Reviews.Application.Interfaces;
using ECommerce.Reviews.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Reviews.Infrastructure.Services;

public class CatalogService(ReviewsDbContext reviewsDbContext) : ICatalogService
{
    public Task<bool> ProductExistsAsync(Guid productId, CancellationToken cancellationToken = default)
        => reviewsDbContext.Products
            .AsNoTracking()
            .AnyAsync(product => product.Id == productId && product.IsActive, cancellationToken);
}
