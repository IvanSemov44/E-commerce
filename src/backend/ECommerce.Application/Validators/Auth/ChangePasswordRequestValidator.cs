using FluentValidation;
using ECommerce.Application.DTOs.Auth;

namespace ECommerce.Application.Validators.Auth;

/// <summary>
/// Validator for ChangePasswordRequest - validates authenticated password changes.
/// </summary>
public class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(x => x.OldPassword)
            .NotEmpty().WithMessage("Current password is required");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("New password is required")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters")
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter")
            .Matches("[0-9]").WithMessage("Password must contain at least one digit");

        RuleFor(x => x.NewPassword)
            .NotEqual(x => x.OldPassword)
            .WithMessage("New password must be different from current password");
    }
}
