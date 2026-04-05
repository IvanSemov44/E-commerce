namespace ECommerce.Promotions.Application.DTOs;

public class UpdatePromoCodeRequestDto
{
    public bool? IsActive { get; set; }
    public string? DiscountType { get; set; }
    public decimal? DiscountValue { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int? MaxUses { get; set; }
    public decimal? MinOrderAmount { get; set; }
    public decimal? MaxDiscountAmount { get; set; }
}
