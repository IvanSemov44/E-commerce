using FluentValidation;
using ECommerce.Contracts.DTOs.Categories;

namespace ECommerce.Contracts.Validators.Categories;

/// <summary>
/// Validator for CreateCategoryDto - validates new category creation.
/// </summary>
public class CreateCategoryDtoValidator : AbstractValidator<CreateCategoryDto>
{
    public CreateCategoryDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Category name is required")
            .Length(1, 100).WithMessage("Category name must be between 1 and 100 characters");

        RuleFor(x => x.Slug)
            .NotEmpty().WithMessage("Category slug is required")
            .Length(1, 100).WithMessage("Slug must be between 1 and 100 characters")
            .Matches("^[a-z0-9-]+$").WithMessage("Slug must contain only lowercase letters, numbers, and hyphens");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters")
            .When(x => x.Description != null);

        RuleFor(x => x.ImageUrl)
            .Must(x => Uri.TryCreate(x, UriKind.Absolute, out _))
            .WithMessage("Image URL must be a valid URL")
            .When(x => !string.IsNullOrEmpty(x.ImageUrl));
    }
}

