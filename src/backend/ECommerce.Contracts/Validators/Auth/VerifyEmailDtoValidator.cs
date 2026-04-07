using FluentValidation;
using ECommerce.Contracts.DTOs.Auth;

namespace ECommerce.Contracts.Validators.Auth;

/// <summary>
/// Validator for VerifyEmailDto - validates email verification tokens.
/// </summary>
public class VerifyEmailDtoValidator : AbstractValidator<VerifyEmailDto>
{
    public VerifyEmailDtoValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required");

        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Verification token is required")
            .MinimumLength(10).WithMessage("Invalid verification token");
    }
}

