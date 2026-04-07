using FluentValidation;
using ECommerce.Contracts.DTOs.Orders;

namespace ECommerce.Contracts.Validators.Orders;

public class OrderQueryParametersValidator : AbstractValidator<OrderQueryParameters>
{
    public OrderQueryParametersValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}

