using FluentValidation;
using ECommerce.Application.DTOs.Wishlist;

namespace ECommerce.Application.Validators.Wishlist;

/// <summary>
/// Validator for AddToWishlistDto - validates wishlist addition requests.
/// </summary>
public class AddToWishlistDtoValidator : AbstractValidator<AddToWishlistDto>
{
    public AddToWishlistDtoValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("Product ID is required");
    }
}
