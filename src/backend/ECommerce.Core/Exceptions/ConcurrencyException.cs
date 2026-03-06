using ECommerce.Core.Exceptions.Base;

namespace ECommerce.Core.Exceptions;

/// <summary>
/// Exception thrown when a database concurrency conflict is detected during an update operation.
/// Maps to HTTP 409 Conflict status code.
/// Used when DbUpdateConcurrencyException occurs (e.g., row version mismatch on Order or Cart).
/// </summary>
public sealed class ConcurrencyException(string resourceType, string resourceId = "") 
    : ConflictException($"The {resourceType} was modified by another user. Please refresh and try again.") 
{
    /// <summary>
    /// The type of resource affected by the concurrency conflict (e.g., "Order", "Cart", "Product").
    /// </summary>
    public string ResourceType { get; } = resourceType;

    /// <summary>
    /// The identifier of the resource affected by the concurrency conflict.
    /// </summary>
    public string ResourceId { get; } = resourceId;
}
