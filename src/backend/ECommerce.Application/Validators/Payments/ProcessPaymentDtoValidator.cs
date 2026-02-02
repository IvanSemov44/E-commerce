using FluentValidation;
using ECommerce.Application.DTOs.Payments;

namespace ECommerce.Application.Validators.Payments;

public class ProcessPaymentDtoValidator : AbstractValidator<ProcessPaymentDto>
{
    private static readonly HashSet<string> AllowedMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        "stripe",
        "paypal",
        "credit_card",
        "card"
    };

    public ProcessPaymentDtoValidator()
    {
        RuleFor(x => x.OrderId)
            .NotEmpty().WithMessage("OrderId is required");

        RuleFor(x => x.PaymentMethod)
            .NotEmpty().WithMessage("PaymentMethod is required")
            .Must(m => AllowedMethods.Contains(m ?? string.Empty))
            .WithMessage("Unsupported payment method");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Amount must be greater than zero");

        When(x => x.PaymentMethod != null && x.PaymentMethod.Equals("credit_card", StringComparison.OrdinalIgnoreCase), () =>
        {
            RuleFor(x => x.CardToken)
                .NotEmpty().WithMessage("CardToken is required for credit card payments");
        });

        When(x => x.PaymentMethod != null && x.PaymentMethod.Equals("paypal", StringComparison.OrdinalIgnoreCase), () =>
        {
            RuleFor(x => x.PayPalEmail)
                .NotEmpty().WithMessage("PayPalEmail is required for PayPal payments")
                .EmailAddress().WithMessage("PayPalEmail must be a valid email address");
        });
    }
}
