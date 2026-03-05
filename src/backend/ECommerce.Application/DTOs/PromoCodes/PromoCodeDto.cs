namespace ECommerce.Application.DTOs.PromoCodes;

public record PromoCodeDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = null!;
    public string DiscountType { get; init; } = null!;
    public decimal DiscountValue { get; init; }
    public decimal? MinOrderAmount { get; init; }
    public int? MaxUses { get; init; }
    public int UsedCount { get; init; }
    public bool IsActive { get; init; }
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
}