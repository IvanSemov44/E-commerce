using System.Threading;
using System.Threading.Tasks;
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Catalog.Application.Errors;
using ECommerce.Catalog.Domain.Interfaces;

namespace ECommerce.Catalog.Application.Commands.ActivateProduct;

public class ActivateProductCommandHandler(
    IProductRepository _products
) : IRequestHandler<ActivateProductCommand, Result>
{
    public async Task<Result> Handle(ActivateProductCommand command, CancellationToken cancellationToken)
    {
        var product = await _products.GetByIdAsync(command.Id, cancellationToken);
        if (product is null)
            return Result.Fail(CatalogApplicationErrors.ProductNotFound);

        var result = product.Activate();
        if (!result.IsSuccess)
            return Result.Fail(result.GetErrorOrThrow());

        return Result.Ok();
    }
}
