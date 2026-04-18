namespace ECommerce.Shopping.Application.Interfaces;

public record ProductPriceInfo(decimal Price, string Currency);

public interface IShoppingProductReader
{
    Task<ProductPriceInfo?> GetProductPriceAsync(Guid productId, CancellationToken ct);
}
