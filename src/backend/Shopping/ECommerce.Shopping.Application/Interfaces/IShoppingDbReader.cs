namespace ECommerce.Shopping.Application.Interfaces;

public record ProductPriceInfo(decimal Price, string Currency);

public interface IShoppingDbReader
{
    Task<ProductPriceInfo?> GetProductPriceAsync(Guid productId, CancellationToken ct);
    Task<bool> ProductExistsAsync(Guid productId, CancellationToken ct);
    Task<bool> IsInStockAsync(Guid productId, int quantity, CancellationToken ct);
}