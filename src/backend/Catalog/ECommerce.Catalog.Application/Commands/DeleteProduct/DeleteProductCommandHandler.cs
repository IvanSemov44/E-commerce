using System.Threading;
using System.Threading.Tasks;
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Catalog.Application.Errors;
using ECommerce.Catalog.Application.Interfaces;
using ECommerce.Catalog.Domain.Interfaces;

namespace ECommerce.Catalog.Application.Commands.DeleteProduct;

public class DeleteProductCommandHandler(
    IProductRepository _products,
    IProductProjectionEventPublisher? _projectionPublisher = null
) : IRequestHandler<DeleteProductCommand, Result>
{
    public async Task<Result> Handle(DeleteProductCommand command, CancellationToken cancellationToken)
    {
        var product = await _products.GetByIdAsync(command.Id, cancellationToken);
        if (product is null)
            return Result.Fail(CatalogApplicationErrors.ProductNotFound);

        product.Delete();
        await _products.DeleteAsync(product, cancellationToken);

        if (_projectionPublisher is not null)
        {
            await _projectionPublisher.PublishProductProjectionUpdatedAsync(
                product.Id,
                product.Name.Value,
                product.Price.Amount,
                true,
                cancellationToken);

            foreach (var image in product.Images)
            {
                await _projectionPublisher.PublishProductImageProjectionUpdatedAsync(
                    image.Id,
                    product.Id,
                    image.Url,
                    image.IsPrimary,
                    true,
                    cancellationToken);
            }
        }

        return Result.Ok();
    }
}
