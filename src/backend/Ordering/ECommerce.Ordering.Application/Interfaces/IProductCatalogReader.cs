namespace ECommerce.Ordering.Application.Interfaces;

public record ProductSnapshot(Guid ProductId, string ProductName, decimal UnitPrice, string? ImageUrl);

public interface IProductCatalogReader
{
    Task<List<ProductSnapshot>> GetProductsAsync(List<Guid> productIds, CancellationToken ct);
}
