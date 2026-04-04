namespace ECommerce.Promotions.Application.DTOs;

public class ValidatePromoCodeRequestDto
{
    public string Code { get; set; } = null!;
    public decimal OrderAmount { get; set; }
}
