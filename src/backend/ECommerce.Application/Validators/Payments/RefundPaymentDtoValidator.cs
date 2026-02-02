using FluentValidation;
using ECommerce.Application.DTOs.Payments;

namespace ECommerce.Application.Validators.Payments;

public class RefundPaymentDtoValidator : AbstractValidator<RefundPaymentDto>
{
    public RefundPaymentDtoValidator()
    {
        RuleFor(x => x.OrderId)
            .NotEmpty().WithMessage("OrderId is required");

        RuleFor(x => x.Amount)
            .GreaterThan(0).When(x => x.Amount.HasValue).WithMessage("Refund amount must be greater than zero");

        RuleFor(x => x.Reason)
            .MaximumLength(500).WithMessage("Reason must be 500 characters or fewer");
    }
}
