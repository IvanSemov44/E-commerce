using FluentValidation;
using ECommerce.Application.DTOs.Auth;

namespace ECommerce.Application.Validators.Auth;

/// <summary>
/// Validator for ForgotPasswordRequest - initiates password recovery flow.
/// </summary>
public class ForgotPasswordRequestValidator : AbstractValidator<ForgotPasswordRequest>
{
    public ForgotPasswordRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format");
    }
}
