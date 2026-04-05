using FluentValidation;

namespace ECommerce.Reviews.Application.Commands;

public class FlagReviewCommandValidator : AbstractValidator<FlagReviewCommand>
{
    public FlagReviewCommandValidator()
    {
        RuleFor(x => x.ReviewId).NotEmpty();
        RuleFor(x => x.Reason)
            .NotEmpty()
            .MaximumLength(500)
            .When(x => !string.IsNullOrWhiteSpace(x.Reason));
    }
}