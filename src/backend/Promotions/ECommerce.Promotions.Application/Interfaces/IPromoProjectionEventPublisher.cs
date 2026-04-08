namespace ECommerce.Promotions.Application.Interfaces;

public interface IPromoProjectionEventPublisher
{
    Task PublishPromoProjectionUpdatedAsync(
        Guid promoCodeId,
        string code,
        decimal discountValue,
        bool isActive,
        bool isDeleted,
        CancellationToken cancellationToken = default);
}
