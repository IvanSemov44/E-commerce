using ECommerce.Reviews.Application.DTOs;
using FluentValidation;

namespace ECommerce.Reviews.Application.Validators;

public class UpdateReviewRequestDtoValidator : AbstractValidator<UpdateReviewRequestDto>
{
    public UpdateReviewRequestDtoValidator()
    {
        RuleFor(x => x.Rating).InclusiveBetween(1, 5).When(x => x.Rating.HasValue);
        RuleFor(x => x.Title)
            .MaximumLength(100)
            .When(x => !string.IsNullOrWhiteSpace(x.Title));
        RuleFor(x => x.Comment)
            .MaximumLength(1000)
            .When(x => !string.IsNullOrWhiteSpace(x.Comment));
    }
}
