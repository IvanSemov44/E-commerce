using FluentValidation;
using ECommerce.Application.DTOs.PromoCodes;

namespace ECommerce.Application.Validators.PromoCodes;

public class PromoCodeQueryParametersValidator : AbstractValidator<PromoCodeQueryParameters>
{
    public PromoCodeQueryParametersValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
