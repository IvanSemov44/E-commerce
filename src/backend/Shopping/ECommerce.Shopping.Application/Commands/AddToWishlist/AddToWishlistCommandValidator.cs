namespace ECommerce.Shopping.Application.Commands.AddToWishlist;

public class AddToWishlistCommandValidator : AbstractValidator<AddToWishlistCommand>
{
    public AddToWishlistCommandValidator()
    {
        RuleFor(request => request.UserId)
            .NotEmpty();

        RuleFor(request => request.ProductId)
            .NotEmpty();
    }
}
