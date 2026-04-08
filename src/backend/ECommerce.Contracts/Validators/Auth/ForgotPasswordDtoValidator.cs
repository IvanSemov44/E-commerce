using FluentValidation;
using ECommerce.Contracts.DTOs.Auth;

namespace ECommerce.Contracts.Validators.Auth;

/// <summary>
/// Validator for ForgotPasswordDto - initiates password recovery flow.
/// </summary>
public class ForgotPasswordDtoValidator : AbstractValidator<ForgotPasswordDto>
{
    public ForgotPasswordDtoValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format");
    }
}

