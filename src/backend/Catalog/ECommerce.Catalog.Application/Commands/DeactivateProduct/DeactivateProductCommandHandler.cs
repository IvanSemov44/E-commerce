using System.Threading;
using System.Threading.Tasks;
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Catalog.Application.Errors;
using ECommerce.Catalog.Domain.Interfaces;

namespace ECommerce.Catalog.Application.Commands.DeactivateProduct;

public class DeactivateProductCommandHandler(
    IProductRepository _products
) : IRequestHandler<DeactivateProductCommand, Result>
{
    public async Task<Result> Handle(DeactivateProductCommand command, CancellationToken cancellationToken)
    {
        var product = await _products.GetByIdAsync(command.Id, cancellationToken);
        if (product is null)
            return Result.Fail(CatalogApplicationErrors.ProductNotFound);

        var result = product.Deactivate();
        if (!result.IsSuccess)
            return Result.Fail(result.GetErrorOrThrow());

        await _products.UpdateAsync(product, cancellationToken);
        return Result.Ok();
    }
}
