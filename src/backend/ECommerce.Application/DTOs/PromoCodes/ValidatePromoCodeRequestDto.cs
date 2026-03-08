using System.Text.Json.Serialization;

namespace ECommerce.Application.DTOs.PromoCodes;

public class ValidatePromoCodeRequestDto
{
    [JsonPropertyName("code")]
    public string Code { get; set; } = null!;

    [JsonPropertyName("orderAmount")]
    public decimal OrderAmount { get; set; } = 0m;
}
