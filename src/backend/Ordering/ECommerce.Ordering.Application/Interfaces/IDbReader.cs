namespace ECommerce.Ordering.Application.Interfaces;

public record ProductSnapshot(Guid ProductId, string ProductName, decimal UnitPrice, string? ImageUrl);

public record ShippingAddressSnapshot(string Street, string City, string Country, string? PostalCode);

public interface IDbReader
{
    Task<List<ProductSnapshot>> GetProductsAsync(List<Guid> productIds, CancellationToken ct);
    Task<(decimal Discount, Guid PromoCodeId)?> GetPromoCodeAsync(string code, CancellationToken ct);
    Task<ShippingAddressSnapshot?> GetShippingAddressAsync(Guid userId, Guid addressId, CancellationToken ct);
}
