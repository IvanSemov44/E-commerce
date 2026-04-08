using FluentValidation;

namespace ECommerce.Shopping.Application.Commands.ClearWishlist;

public class ClearWishlistCommandValidator : AbstractValidator<ClearWishlistCommand>
{
    public ClearWishlistCommandValidator()
    {
        RuleFor(request => request.UserId)
            .NotEmpty();
    }
}