using System;
using ECommerce.SharedKernel.Domain;

namespace ECommerce.Catalog.Domain.Aggregates.Product.Events;

public record ProductDeactivatedEvent(Guid ProductId) : DomainEventBase;
