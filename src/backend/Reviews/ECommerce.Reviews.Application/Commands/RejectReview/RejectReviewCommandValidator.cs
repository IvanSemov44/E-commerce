using FluentValidation;

namespace ECommerce.Reviews.Application.Commands;

public class RejectReviewCommandValidator : AbstractValidator<RejectReviewCommand>
{
    public RejectReviewCommandValidator()
    {
        RuleFor(x => x.ReviewId).NotEmpty();
    }
}