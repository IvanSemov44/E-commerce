using FluentValidation;
using ECommerce.Application.DTOs.Auth;

namespace ECommerce.Application.Validators.Auth;

/// <summary>
/// Validator for RefreshTokenDto - ensures valid token is provided.
/// </summary>
public class RefreshTokenDtoValidator : AbstractValidator<RefreshTokenDto>
{
    public RefreshTokenDtoValidator()
    {
        // Token is REQUIRED - must be provided (not null)
        // If null, it means the property was missing from the JSON
        RuleFor(x => x.Token)
            .NotNull()
            .WithMessage("Token is required");

        // If token is provided as empty string, let it through to service
        // which will validate and return 401 Unauthorized
        // If token is provided but too short, let it through to service
        RuleFor(x => x.Token)
            .MinimumLength(10).When(x => !string.IsNullOrEmpty(x.Token))
            .WithMessage("Refresh token must be valid");
    }
}
