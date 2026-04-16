using ECommerce.Catalog.Infrastructure.Persistence;
using ECommerce.Catalog.Domain.Aggregates.Product;
using ECommerce.Reviews.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.API.Services;

public sealed record ReviewsProductProjectionBackfillResult(
    int CatalogProductCount,
    int InsertedCount,
    int UpdatedCount);

public sealed class ReviewsProductProjectionBackfillService(
    CatalogDbContext catalogDbContext,
    ReviewsDbContext reviewsDbContext,
    ILogger<ReviewsProductProjectionBackfillService> logger)
{
    private readonly CatalogDbContext _catalogDbContext = catalogDbContext;
    private readonly ReviewsDbContext _reviewsDbContext = reviewsDbContext;
    private readonly ILogger<ReviewsProductProjectionBackfillService> _logger = logger;

    public async Task<ReviewsProductProjectionBackfillResult> BackfillAsync(CancellationToken cancellationToken = default)
    {
        await EnsureProjectionTableExistsAsync(cancellationToken);

        var catalogProducts = await _catalogDbContext.Products
            .AsNoTracking()
            .Select(x => new { x.Id, IsActive = x.Status == ProductStatus.Active })
            .ToListAsync(cancellationToken);

        var reviewProductsById = await _reviewsDbContext.Products
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        int insertedCount = 0;
        int updatedCount = 0;
        DateTime utcNow = DateTime.UtcNow;

        foreach (var catalogProduct in catalogProducts)
        {
            if (!reviewProductsById.TryGetValue(catalogProduct.Id, out var reviewProjection))
            {
                _reviewsDbContext.Products.Add(new ProductReadModel
                {
                    Id = catalogProduct.Id,
                    IsActive = catalogProduct.IsActive,
                    UpdatedAt = utcNow
                });
                insertedCount++;
                continue;
            }

            if (reviewProjection.IsActive != catalogProduct.IsActive)
            {
                reviewProjection.IsActive = catalogProduct.IsActive;
                reviewProjection.UpdatedAt = utcNow;
                updatedCount++;
            }
        }

        if (insertedCount > 0 || updatedCount > 0)
        {
            await _reviewsDbContext.SaveChangesAsync(cancellationToken);
        }

        var result = new ReviewsProductProjectionBackfillResult(
            catalogProducts.Count,
            insertedCount,
            updatedCount);

        _logger.LogInformation(
            "Reviews projection backfill completed. CatalogProducts={CatalogProductCount}, Inserted={InsertedCount}, Updated={UpdatedCount}",
            result.CatalogProductCount,
            result.InsertedCount,
            result.UpdatedCount);

        return result;
    }

    private async Task EnsureProjectionTableExistsAsync(CancellationToken cancellationToken)
    {
        if (!_reviewsDbContext.Database.IsRelational())
            return;

        const string sql = """
            CREATE TABLE IF NOT EXISTS "ReviewProductProjections" (
                "Id" uuid NOT NULL,
                "IsActive" boolean NOT NULL,
                "UpdatedAt" timestamp with time zone NOT NULL,
                CONSTRAINT "PK_ReviewProductProjections" PRIMARY KEY ("Id")
            );
            """;

        await _reviewsDbContext.Database.ExecuteSqlRawAsync(sql, cancellationToken);
    }
}
