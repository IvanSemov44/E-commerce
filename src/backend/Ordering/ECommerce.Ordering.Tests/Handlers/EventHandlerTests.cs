using ECommerce.Contracts;
using ECommerce.Ordering.Domain.Aggregates.Order;
using ECommerce.Ordering.Domain.Events;
using ECommerce.Ordering.Infrastructure.EventHandlers;
using ECommerce.Ordering.Infrastructure.Integration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ECommerce.Ordering.Tests.Handlers;

[TestClass]
public class OrderPlacedEventHandlerTests
{
    [TestMethod]
    public async Task Handle_OrderPlacedEvent_EnqueuesOrderPlacedIntegrationEvent()
    {
        var writer = new FakeOrderingOutboxEventWriter();
        var handler = new OrderPlacedEventHandler(writer);

        var items = new List<OrderItemData>
        {
            new(Guid.NewGuid(), "Widget", 50m, 2, null)
        };
        var domainEvent = new OrderPlacedEvent(Guid.NewGuid(), Guid.NewGuid(), 100m, items);

        await handler.Handle(domainEvent, default);

        writer.Enqueued.Count.ShouldBe(1);
        writer.Enqueued[0].ShouldBeOfType<OrderPlacedIntegrationEvent>();

        var ie = (OrderPlacedIntegrationEvent)writer.Enqueued[0];
        ie.OrderId.ShouldBe(domainEvent.OrderId);
        ie.CustomerId.ShouldBe(domainEvent.UserId);
        ie.TotalAmount.ShouldBe(domainEvent.Total);
        ie.ProductIds.Length.ShouldBe(1);
        ie.Quantities.Length.ShouldBe(1);
        ie.Quantities[0].ShouldBe(2);
    }
}

[TestClass]
public class OrderDeliveredEventHandlerTests
{
    [TestMethod]
    public async Task Handle_OrderDeliveredEvent_EnqueuesOrderDeliveredIntegrationEvent()
    {
        var writer = new FakeOrderingOutboxEventWriter();
        var handler = new OrderDeliveredEventHandler(writer);

        var productIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
        var domainEvent = new OrderDeliveredEvent(Guid.NewGuid(), Guid.NewGuid(), productIds);

        await handler.Handle(domainEvent, default);

        writer.Enqueued.Count.ShouldBe(1);
        writer.Enqueued[0].ShouldBeOfType<OrderDeliveredIntegrationEvent>();

        var ie = (OrderDeliveredIntegrationEvent)writer.Enqueued[0];
        ie.OrderId.ShouldBe(domainEvent.OrderId);
        ie.UserId.ShouldBe(domainEvent.UserId);
        ie.ProductIds.Length.ShouldBe(2);
        ie.CorrelationId.ShouldBe(domainEvent.OrderId);
    }
}

sealed class FakeOrderingOutboxEventWriter : IOrderingOutboxEventWriter
{
    public List<IntegrationEvent> Enqueued { get; } = new();

    public Task EnqueueAsync(IntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        Enqueued.Add(integrationEvent);
        return Task.CompletedTask;
    }
}
