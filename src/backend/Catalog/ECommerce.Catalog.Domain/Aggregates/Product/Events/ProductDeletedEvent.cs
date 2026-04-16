using System;
using System.Collections.Generic;
using ECommerce.SharedKernel.Domain;

namespace ECommerce.Catalog.Domain.Aggregates.Product.Events;

public record ProductImageSnapshot(Guid ImageId, string Url, bool IsPrimary);

public record ProductDeletedEvent(
    Guid ProductId,
    string Name,
    decimal Price,
    IReadOnlyList<ProductImageSnapshot> Images) : DomainEventBase;
