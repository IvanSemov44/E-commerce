namespace ECommerce.Shopping.Application.Queries.IsProductInWishlist;

public class IsProductInWishlistQueryValidator : AbstractValidator<IsProductInWishlistQuery>
{
    public IsProductInWishlistQueryValidator()
    {
        RuleFor(request => request.UserId)
            .NotEmpty();

        RuleFor(request => request.ProductId)
            .NotEmpty();
    }
}