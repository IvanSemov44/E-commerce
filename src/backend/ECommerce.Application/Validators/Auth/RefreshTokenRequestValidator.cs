using FluentValidation;
using ECommerce.Application.DTOs.Auth;

namespace ECommerce.Application.Validators.Auth;

/// <summary>
/// Validator for RefreshTokenRequest - ensures valid token is provided.
/// </summary>
public class RefreshTokenRequestValidator : AbstractValidator<RefreshTokenRequest>
{
    public RefreshTokenRequestValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Refresh token is required")
            .MinimumLength(10).WithMessage("Refresh token must be valid");
    }
}
