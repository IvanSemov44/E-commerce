namespace ECommerce.Application.DTOs.PromoCodes;

public class ValidatePromoCodeDto
{
    public bool IsValid { get; set; }
    public string? Message { get; set; }
    public decimal DiscountAmount { get; set; }
    public PromoCodeDto? PromoCode { get; set; }
}