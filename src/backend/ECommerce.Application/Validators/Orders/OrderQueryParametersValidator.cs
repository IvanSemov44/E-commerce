using FluentValidation;
using ECommerce.Application.DTOs.Orders;

namespace ECommerce.Application.Validators.Orders;

public class OrderQueryParametersValidator : AbstractValidator<OrderQueryParameters>
{
    public OrderQueryParametersValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
