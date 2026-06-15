using FluentValidation;

namespace ECommerce.Catalog.Application.Commands;

public class DeactivateProductCommandValidator : AbstractValidator<DeactivateProductCommand>
{
    public DeactivateProductCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
