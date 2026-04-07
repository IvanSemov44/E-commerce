using FluentValidation;
using ECommerce.Contracts.DTOs.Reviews;

namespace ECommerce.Contracts.Validators.Reviews;

/// <summary>
/// Validator for UpdateReviewDto - validates product review updates with optional fields.
/// </summary>
public class UpdateReviewDtoValidator : AbstractValidator<UpdateReviewDto>
{
    public UpdateReviewDtoValidator()
    {
        RuleFor(x => x.Title)
            .MaximumLength(100).WithMessage("Title must not exceed 100 characters")
            .When(x => x.Title != null);

        RuleFor(x => x.Comment)
            .Length(10, 1000).WithMessage("Comment must be between 10 and 1000 characters")
            .When(x => x.Comment != null);

        RuleFor(x => x.Rating)
            .InclusiveBetween(1, 5).WithMessage("Rating must be between 1 and 5")
            .When(x => x.Rating.HasValue);
    }
}

