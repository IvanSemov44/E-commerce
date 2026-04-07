namespace ECommerce.Payments.Application.DTOs;

public record SupportedPaymentMethodsResponseDto
{
    public List<string> Methods { get; init; } = new();
}
