using ECommerce.Shopping.Application.DTOs;

namespace ECommerce.Shopping.Application.Validators.Wishlist;

public class AddToWishlistDtoValidator : AbstractValidator<AddToWishlistDto>
{
    public AddToWishlistDtoValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("Product ID is required");
    }
}
