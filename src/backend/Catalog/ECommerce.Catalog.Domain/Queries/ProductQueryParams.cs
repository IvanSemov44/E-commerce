using System;

namespace ECommerce.Catalog.Domain.Queries;

public record ProductQueryParams(
    int      Page,
    int      PageSize,
    Guid?    CategoryId = null,
    string?  Search     = null,
    decimal? MinPrice   = null,
    decimal? MaxPrice   = null,
    decimal? MinRating  = null,
    bool?    IsFeatured = null,
    string?  SortBy     = null
);
