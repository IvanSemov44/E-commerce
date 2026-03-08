using FluentValidation;
using ECommerce.Application.DTOs.Users;

namespace ECommerce.Application.Validators.Users;

/// <summary>
/// Validator for ChangePasswordDto - validates password change requests.
/// </summary>
public class ChangePasswordDtoValidator : AbstractValidator<ChangePasswordDto>
{
    public ChangePasswordDtoValidator()
    {
        RuleFor(x => x.OldPassword)
            .NotEmpty().WithMessage("Old password is required");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("New password is required")
            .MinimumLength(8).WithMessage("New password must be at least 8 characters")
            .MaximumLength(100).WithMessage("New password cannot exceed 100 characters")
            .Matches("[A-Z]").WithMessage("New password must contain at least one uppercase letter")
            .Matches("[a-z]").WithMessage("New password must contain at least one lowercase letter")
            .Matches("[0-9]").WithMessage("New password must contain at least one digit")
            .NotEqual(x => x.OldPassword).WithMessage("New password must be different from the old password");

        RuleFor(x => x.ConfirmPassword)
            .NotEmpty().WithMessage("Password confirmation is required")
            .Equal(x => x.NewPassword).WithMessage("Password confirmation does not match new password");
    }
}
