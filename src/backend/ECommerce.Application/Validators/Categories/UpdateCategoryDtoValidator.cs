using FluentValidation;
using ECommerce.Application.DTOs.Categories;

namespace ECommerce.Application.Validators.Categories;

/// <summary>
/// Validator for UpdateCategoryDto - validates category updates with optional fields.
/// </summary>
public class UpdateCategoryDtoValidator : AbstractValidator<UpdateCategoryDto>
{
    public UpdateCategoryDtoValidator()
    {
        RuleFor(x => x.Name)
            .Length(1, 100).WithMessage("Category name must be between 1 and 100 characters")
            .When(x => x.Name != null);

        RuleFor(x => x.Slug)
            .Length(1, 100).WithMessage("Slug must be between 1 and 100 characters")
            .Matches("^[a-z0-9-]+$").WithMessage("Slug must contain only lowercase letters, numbers, and hyphens")
            .When(x => x.Slug != null);

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters")
            .When(x => x.Description != null);

        RuleFor(x => x.ImageUrl)
            .Must(x => Uri.TryCreate(x, UriKind.Absolute, out _))
            .WithMessage("Image URL must be a valid URL")
            .When(x => !string.IsNullOrEmpty(x.ImageUrl));
    }
}
