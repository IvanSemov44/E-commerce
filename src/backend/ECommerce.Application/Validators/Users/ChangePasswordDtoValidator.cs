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
            .Length(8, 100).WithMessage("New password must be between 8 and 100 characters");

        RuleFor(x => x.ConfirmPassword)
            .NotEmpty().WithMessage("Password confirmation is required")
            .Equal(x => x.NewPassword).WithMessage("Password confirmation does not match new password");
    }
}
