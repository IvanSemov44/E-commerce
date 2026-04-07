using FluentValidation;

namespace ECommerce.Shopping.Application.Queries.GetWishlist;

public class GetWishlistQueryValidator : AbstractValidator<GetWishlistQuery>
{
    public GetWishlistQueryValidator()
    {
        RuleFor(request => request.UserId)
            .NotEmpty();
    }
}