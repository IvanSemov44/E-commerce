using System.Text.Json.Serialization;

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

public record PromoCodeDetailDto
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
    public decimal? MaxDiscountAmount { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public class CreatePromoCodeDto
{
    public string Code { get; set; } = null!;
    public string DiscountType { get; set; } = null!;
    public decimal DiscountValue { get; set; }
    public decimal? MinOrderAmount { get; set; }
    public decimal? MaxDiscountAmount { get; set; }
    public int? MaxUses { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsActive { get; set; } = true;
}

public class UpdatePromoCodeDto
{
    public string? Code { get; set; }
    public string? DiscountType { get; set; }
    public decimal? DiscountValue { get; set; }
    public decimal? MinOrderAmount { get; set; }
    public decimal? MaxDiscountAmount { get; set; }
    public int? MaxUses { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool? IsActive { get; set; }
}

public class ValidatePromoCodeRequestDto
{
    [JsonPropertyName("code")]
    public string Code { get; set; } = null!;

    [JsonPropertyName("orderAmount")]
    public decimal OrderAmount { get; set; } = 0m;
}

public class ValidatePromoCodeDto
{
    public bool IsValid { get; set; }
    public string? Message { get; set; }
    public decimal DiscountAmount { get; set; }
    public PromoCodeDto? PromoCode { get; set; }
}
