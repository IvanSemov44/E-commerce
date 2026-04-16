using System.Threading;
using System.Threading.Tasks;
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Catalog.Application.Errors;
using ECommerce.Catalog.Domain.Interfaces;

namespace ECommerce.Catalog.Application.Commands.UpdateProductStock;

public class UpdateProductStockCommandHandler(
    IProductRepository _products
) : IRequestHandler<UpdateProductStockCommand, Result>
{
    public async Task<Result> Handle(UpdateProductStockCommand command, CancellationToken cancellationToken)
    {
        var product = await _products.GetByIdAsync(command.Id, cancellationToken);
        if (product is null)
            return Result.Fail(CatalogApplicationErrors.ProductNotFound);

        var result = product.SetStock(command.Quantity);
        if (!result.IsSuccess)
            return Result.Fail(result.GetErrorOrThrow());

        return Result.Ok();
    }
}
