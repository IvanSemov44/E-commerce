using System;
using ECommerce.Catalog.Domain.ValueObjects;
using ECommerce.SharedKernel.Domain;

namespace ECommerce.Catalog.Domain.Aggregates.Product.Events;

public record ProductPriceChangedEvent(Guid ProductId, string Name, Money OldPrice, Money NewPrice) : DomainEventBase;
