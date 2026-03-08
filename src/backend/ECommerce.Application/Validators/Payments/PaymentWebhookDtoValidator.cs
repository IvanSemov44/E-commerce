using ECommerce.Application.DTOs.Payments;
using FluentValidation;

namespace ECommerce.Application.Validators.Payments;

public class PaymentWebhookDtoValidator : AbstractValidator<PaymentWebhookDto>
{
    public PaymentWebhookDtoValidator()
    {
        RuleFor(x => x.EventType)
            .NotEmpty().WithMessage("EventType is required")
            .MaximumLength(100).WithMessage("EventType must be 100 characters or fewer");

        RuleFor(x => x.PaymentIntentId)
            .NotEmpty().WithMessage("PaymentIntentId is required")
            .MaximumLength(255).WithMessage("PaymentIntentId must be 255 characters or fewer");

        RuleFor(x => x.Amount)
            .NotNull().WithMessage("Amount is required")
            .GreaterThanOrEqualTo(0m).WithMessage("Amount must be zero or greater");

        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("Status is required")
            .MaximumLength(50).WithMessage("Status must be 50 characters or fewer");

        RuleFor(x => x.Currency)
            .NotEmpty().WithMessage("Currency is required")
            .Length(3).WithMessage("Currency must be a 3-letter ISO code")
            .Matches("^[A-Za-z]{3}$").WithMessage("Currency must contain only letters");

        RuleFor(x => x.Timestamp)
            .NotNull().WithMessage("Timestamp is required")
            .GreaterThan(0).WithMessage("Timestamp must be a valid Unix timestamp");
    }
}
