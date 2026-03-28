using FluentValidation;
namespace ECommerce.Catalog.Application.Commands.AddProductImage;

public class AddProductImageCommandValidator : AbstractValidator<AddProductImageCommand>
{
    public AddProductImageCommandValidator()
    {
        RuleFor(x => x.Url).NotEmpty().MaximumLength(2000);
    }
}
