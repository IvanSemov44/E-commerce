using FluentValidation;
using ECommerce.Application.DTOs.Orders;

namespace ECommerce.Application.Validators.Orders;

/// <summary>
/// Validator for UpdateOrderStatusDto - validates order status transitions.
/// </summary>
public class UpdateOrderStatusDtoValidator : AbstractValidator<UpdateOrderStatusDto>
{
    private static readonly string[] ValidStatuses = { "pending", "confirmed", "processing", "shipped", "delivered", "cancelled", "refunded" };

    public UpdateOrderStatusDtoValidator()
    {
        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("Status is required")
            .Must(s => ValidStatuses.Contains(s.ToLowerInvariant()))
            .WithMessage($"Status must be one of: {string.Join(", ", ValidStatuses)}");
    }
}
