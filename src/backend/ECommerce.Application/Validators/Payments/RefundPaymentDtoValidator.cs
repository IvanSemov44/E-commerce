using FluentValidation;
using ECommerce.Application.DTOs.Payments;

namespace ECommerce.Application.Validators.Payments;

public class RefundPaymentDtoValidator : AbstractValidator<RefundPaymentDto>
{
    public RefundPaymentDtoValidator()
    {
        // Note: OrderId is set by the controller from the route parameter,
        // so we don't validate it here. The service layer will validate that the order exists.

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Refund amount must be greater than zero");

        RuleFor(x => x.Reason)
            .MaximumLength(500).WithMessage("Reason must be 500 characters or fewer");
    }
}
