namespace ECommerce.Shopping.Application.Interfaces;

public interface IStockAvailabilityReader
{
    Task<bool> IsInStockAsync(Guid productId, int quantity, CancellationToken ct);
}
