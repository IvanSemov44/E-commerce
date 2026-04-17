namespace ECommerce.Reviews.Application.Commands.RejectReview;

public class RejectReviewCommandValidator : AbstractValidator<RejectReviewCommand>
{
    public RejectReviewCommandValidator()
    {
        RuleFor(x => x.ReviewId).NotEmpty();
    }
}
