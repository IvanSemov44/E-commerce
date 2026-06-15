using System.Threading;
using System.Threading.Tasks;
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Catalog.Application.Errors;
using ECommerce.Catalog.Domain.Interfaces;

namespace ECommerce.Catalog.Application.Commands;

public class SetPrimaryImageCommandHandler(
    IProductRepository _products
) : IRequestHandler<SetPrimaryImageCommand, Result>
{
    public async Task<Result> Handle(SetPrimaryImageCommand command, CancellationToken cancellationToken)
    {
        var product = await _products.GetByIdAsync(command.ProductId, cancellationToken);
        if (product is null)
            return Result.Fail(CatalogApplicationErrors.ProductNotFound);

        var setResult = product.SetPrimaryImage(command.ImageId);
        if (!setResult.IsSuccess)
            return Result.Fail(setResult.GetErrorOrThrow());

        return Result.Ok();
    }
}
