namespace ECommerce.Ordering.Application.Interfaces;

public interface IPromoCodeLookup
{
    Task<(decimal Discount, Guid PromoCodeId)?> GetPromoCodeAsync(string code, CancellationToken ct);
}
