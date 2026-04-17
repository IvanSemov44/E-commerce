using ECommerce.Reviews.Application.DTOs;
using FluentValidation;

namespace ECommerce.API.Features.Reviews.Validators;

public class CreateReviewRequestDtoValidator : AbstractValidator<CreateReviewRequestDto>
{
    public CreateReviewRequestDtoValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.Rating).InclusiveBetween(1, 5);
        RuleFor(x => x.Comment).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.Title)
            .MaximumLength(100)
            .When(x => !string.IsNullOrWhiteSpace(x.Title));
    }
}
