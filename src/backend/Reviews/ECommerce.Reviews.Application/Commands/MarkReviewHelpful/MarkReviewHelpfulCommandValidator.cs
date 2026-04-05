using FluentValidation;

namespace ECommerce.Reviews.Application.Commands;

public class MarkReviewHelpfulCommandValidator : AbstractValidator<MarkReviewHelpfulCommand>
{
    public MarkReviewHelpfulCommandValidator()
    {
        RuleFor(x => x.ReviewId).NotEmpty();
    }
}