using System.Text.Json;
using ECommerce.Catalog.Application.Interfaces;
using ECommerce.Catalog.Infrastructure.Persistence;
using ECommerce.Contracts;
using ECommerce.Infrastructure.Data;

namespace ECommerce.Catalog.Infrastructure.Services;

/// <summary>
/// Enqueues product projection integration events directly into the Catalog-owned
/// outbox table (catalog.outbox_messages). This makes the enqueue atomic with the
/// aggregate save — both happen inside the same CatalogDbContext.SaveChangesAsync call.
/// </summary>
public sealed class ProductProjectionEventPublisher(CatalogDbContext dbContext) : IProductProjectionEventPublisher
{
    private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    public Task PublishProductProjectionUpdatedAsync(
        Guid productId,
        string name,
        decimal price,
        bool isDeleted,
        CancellationToken cancellationToken = default)
    {
        var evt = new ProductProjectionUpdatedIntegrationEvent(productId, name, price, isDeleted, DateTime.UtcNow);
        Enqueue(evt);
        return Task.CompletedTask;
    }

    public Task PublishProductImageProjectionUpdatedAsync(
        Guid imageId,
        Guid productId,
        string url,
        bool isPrimary,
        bool isDeleted,
        CancellationToken cancellationToken = default)
    {
        var evt = new ProductImageProjectionUpdatedIntegrationEvent(imageId, productId, url, isPrimary, isDeleted, DateTime.UtcNow);
        Enqueue(evt);
        return Task.CompletedTask;
    }

    private void Enqueue<TEvent>(TEvent evt) where TEvent : IntegrationEvent
    {
        dbContext.OutboxMessages.Add(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            IdempotencyKey = evt.IdempotencyKey,
            EventType = evt.GetType().AssemblyQualifiedName ?? evt.GetType().FullName!,
            EventData = JsonSerializer.Serialize(evt, evt.GetType(), _json),
            CreatedAt = DateTime.UtcNow
        });
    }
}
