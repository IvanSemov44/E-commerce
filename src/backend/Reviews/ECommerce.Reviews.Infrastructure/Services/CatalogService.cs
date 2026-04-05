using ECommerce.Infrastructure.Data;
using ECommerce.Reviews.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Reviews.Infrastructure.Services;

public class CatalogService(AppDbContext db) : ICatalogService
{
    public Task<bool> ProductExistsAsync(Guid productId, CancellationToken cancellationToken = default)
        => db.Products.AsNoTracking().AnyAsync(product => product.Id == productId, cancellationToken);
}