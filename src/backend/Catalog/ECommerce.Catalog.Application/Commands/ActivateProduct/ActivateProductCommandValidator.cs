using FluentValidation;

namespace ECommerce.Catalog.Application.Commands;

public class ActivateProductCommandValidator : AbstractValidator<ActivateProductCommand>
{
    public ActivateProductCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
