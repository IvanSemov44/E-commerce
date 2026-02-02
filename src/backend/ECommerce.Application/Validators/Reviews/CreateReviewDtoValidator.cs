using FluentValidation;
using ECommerce.Application.DTOs.Reviews;

namespace ECommerce.Application.Validators.Reviews;

/// <summary>
/// Validator for CreateReviewDto - validates product review creation.
/// </summary>
public class CreateReviewDtoValidator : AbstractValidator<CreateReviewDto>
{
    public CreateReviewDtoValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("Product ID is required");

        RuleFor(x => x.Title)
            .MaximumLength(100).WithMessage("Title must not exceed 100 characters")
            .When(x => x.Title != null);

        RuleFor(x => x.Comment)
            .NotEmpty().WithMessage("Comment is required")
            .Length(10, 1000).WithMessage("Comment must be between 10 and 1000 characters");

        RuleFor(x => x.Rating)
            .InclusiveBetween(1, 5).WithMessage("Rating must be between 1 and 5");
    }
}
