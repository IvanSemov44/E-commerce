namespace ECommerce.Promotions.Application.DTOs;

public sealed class PromoCodeDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string DiscountType { get; init; } = string.Empty;
    public decimal DiscountValue { get; init; }
    public DateTime? ValidFrom { get; init; }
    public DateTime? ValidUntil { get; init; }
    public int? MaxUses { get; init; }
    public int UsedCount { get; init; }
    public bool IsActive { get; init; }
    public decimal? MinimumOrderAmount { get; init; }
    public decimal? MaxDiscountAmount { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public sealed class PromoCodeListItemDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string DiscountType { get; init; } = string.Empty;
    public decimal DiscountValue { get; init; }
    public bool IsActive { get; init; }
    public int UsedCount { get; init; }
    public int? MaxUses { get; init; }
}

public sealed class ValidatePromoCodeDto
{
    public string Code { get; init; } = string.Empty;
    public decimal OrderAmount { get; init; }
}

public sealed class ValidatePromoCodeResultDto
{
    public Guid? PromoCodeId { get; init; }
    public string Code { get; init; } = string.Empty;
    public bool IsValid { get; init; }
    public decimal DiscountAmount { get; init; }
    public string Message { get; init; } = string.Empty;
}