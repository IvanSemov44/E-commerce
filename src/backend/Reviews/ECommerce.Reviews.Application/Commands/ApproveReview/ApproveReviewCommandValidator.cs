using FluentValidation;

namespace ECommerce.Reviews.Application.Commands;

public class ApproveReviewCommandValidator : AbstractValidator<ApproveReviewCommand>
{
    public ApproveReviewCommandValidator()
    {
        RuleFor(x => x.ReviewId).NotEmpty();
    }
}