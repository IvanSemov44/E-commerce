using System;
using ECommerce.SharedKernel.Domain;

namespace ECommerce.Catalog.Domain.Aggregates.Product.Events;

public record ProductPrimaryImageSetEvent(
    Guid ProductId,
    Guid NewPrimaryImageId,
    string NewPrimaryImageUrl,
    Guid? OldPrimaryImageId,
    string? OldPrimaryImageUrl) : DomainEventBase;
