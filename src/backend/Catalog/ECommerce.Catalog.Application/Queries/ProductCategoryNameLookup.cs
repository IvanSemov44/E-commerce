using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ECommerce.Catalog.Domain.Aggregates.Product;
using ECommerce.Catalog.Domain.Interfaces;

namespace ECommerce.Catalog.Application.Queries;

internal static class ProductCategoryNameLookup
{
    public static async Task<IReadOnlyDictionary<Guid, string>> BuildAsync(
        IReadOnlyList<Product> products,
        ICategoryRepository categories,
        CancellationToken cancellationToken)
    {
        var categoryIds = products.Select(p => p.CategoryId).Distinct().ToArray();
        if (categoryIds.Length == 0)
            return new Dictionary<Guid, string>();

        var categoryList = await categories.GetByIdsAsync(categoryIds, cancellationToken);
        return categoryList.ToDictionary(c => c.Id, c => c.Name.Value);
    }
}
