using FluentValidation;
using ECommerce.Application.DTOs.Auth;

namespace ECommerce.Application.Validators.Auth;

/// <summary>
/// Validator for VerifyEmailRequest - validates email verification tokens.
/// </summary>
public class VerifyEmailRequestValidator : AbstractValidator<VerifyEmailRequest>
{
    public VerifyEmailRequestValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required");

        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Verification token is required")
            .MinimumLength(10).WithMessage("Invalid verification token");
    }
}
