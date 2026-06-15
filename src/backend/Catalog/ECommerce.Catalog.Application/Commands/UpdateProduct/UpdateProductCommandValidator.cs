using System;
using FluentValidation;
namespace ECommerce.Catalog.Application.Commands;

public class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.CategoryId)
            .Must(id => !id.HasValue || id.Value != Guid.Empty)
            .WithMessage("CategoryId must not be empty.");
    }
}
