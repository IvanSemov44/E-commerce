using System;
using ECommerce.SharedKernel.Domain;

namespace ECommerce.Catalog.Domain.Aggregates.Product.Events;

public record ProductImageAddedEvent(
    Guid ProductId,
    Guid ImageId,
    string Url,
    bool IsPrimary) : DomainEventBase;
