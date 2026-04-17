namespace ECommerce.Reviews.Application.Commands.ApproveReview;

public class ApproveReviewCommandValidator : AbstractValidator<ApproveReviewCommand>
{
    public ApproveReviewCommandValidator()
    {
        RuleFor(x => x.ReviewId).NotEmpty();
    }
}
