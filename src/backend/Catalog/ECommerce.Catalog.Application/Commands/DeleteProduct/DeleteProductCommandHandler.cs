using System.Threading;
using System.Threading.Tasks;
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Catalog.Application.Errors;
using ECommerce.Catalog.Domain.Interfaces;

namespace ECommerce.Catalog.Application.Commands.DeleteProduct;

public class DeleteProductCommandHandler(
    IProductRepository _products
) : IRequestHandler<DeleteProductCommand, Result>
{
    public async Task<Result> Handle(DeleteProductCommand command, CancellationToken cancellationToken)
    {
        var product = await _products.GetByIdAsync(command.Id, cancellationToken);
        if (product is null)
            return Result.Fail(CatalogApplicationErrors.ProductNotFound);

        product.Delete();

        return Result.Ok();
    }
}
