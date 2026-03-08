namespace ECommerce.Application.DTOs.PromoCodes;

public record ValidatePromoCodeDto
{
    public bool IsValid { get; init; }
    public string? Message { get; init; }
    public decimal DiscountAmount { get; init; }
    public PromoCodeDto? PromoCode { get; init; }
}
