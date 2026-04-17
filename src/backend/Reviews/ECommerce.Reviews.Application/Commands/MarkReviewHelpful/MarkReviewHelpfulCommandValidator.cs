namespace ECommerce.Reviews.Application.Commands.MarkReviewHelpful;

public class MarkReviewHelpfulCommandValidator : AbstractValidator<MarkReviewHelpfulCommand>
{
    public MarkReviewHelpfulCommandValidator()
    {
        RuleFor(x => x.ReviewId).NotEmpty();
    }
}
