using FluentValidation;
using ECommerce.Contracts.DTOs.Users;

namespace ECommerce.Contracts.Validators.Users;

/// <summary>
/// Validator for UpdateProfileDto - validates user profile updates.
/// </summary>
public class UpdateProfileDtoValidator : AbstractValidator<UpdateProfileDto>
{
    public UpdateProfileDtoValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required")
            .Length(1, 50).WithMessage("First name must be between 1 and 50 characters");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required")
            .Length(1, 50).WithMessage("Last name must be between 1 and 50 characters");

        RuleFor(x => x.Phone)
            .Matches(@"^[\d\s\-\+\(\)]*$").WithMessage("Invalid phone number format")
            .MaximumLength(20).WithMessage("Phone number must not exceed 20 characters")
            .When(x => !string.IsNullOrEmpty(x.Phone));

        RuleFor(x => x.AvatarUrl)
            .Must(x => Uri.TryCreate(x, UriKind.Absolute, out _))
            .WithMessage("Avatar URL must be a valid URL")
            .MaximumLength(500).WithMessage("Avatar URL must not exceed 500 characters")
            .When(x => !string.IsNullOrEmpty(x.AvatarUrl));
    }
}

