using ECommerce.Payments.Application.DTOs;
using FluentValidation;

namespace ECommerce.Payments.Application.Validators;

public class RefundPaymentDtoValidator : AbstractValidator<RefundPaymentDto>
{
    public RefundPaymentDtoValidator()
    {
        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Refund amount must be greater than zero");

        RuleFor(x => x.Reason)
            .MaximumLength(500).WithMessage("Reason must be 500 characters or fewer");
    }
}
