using FluentValidation;
using ECommerce.Application.DTOs.Users;

namespace ECommerce.Application.Validators.Users;

/// <summary>
/// Validator for UserPreferencesDto - validates user preference updates.
/// </summary>
public class UserPreferencesDtoValidator : AbstractValidator<UserPreferencesDto>
{
    public UserPreferencesDtoValidator()
    {
        RuleFor(x => x.Language)
            .NotEmpty().WithMessage("Language is required")
            .MaximumLength(10).WithMessage("Language must not exceed 10 characters");

        RuleFor(x => x.Currency)
            .NotEmpty().WithMessage("Currency is required")
            .Length(3).WithMessage("Currency must be a 3-letter ISO code")
            .Matches("^[A-Z]{3}$").WithMessage("Currency must contain only uppercase letters");
    }
}
