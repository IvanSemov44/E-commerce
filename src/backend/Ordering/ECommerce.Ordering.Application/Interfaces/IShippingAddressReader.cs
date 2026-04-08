namespace ECommerce.Ordering.Application.Interfaces;

public record ShippingAddressSnapshot(string Street, string City, string Country, string? PostalCode);

public interface IShippingAddressReader
{
    Task<ShippingAddressSnapshot?> GetShippingAddressAsync(Guid userId, Guid addressId, CancellationToken ct);
}
