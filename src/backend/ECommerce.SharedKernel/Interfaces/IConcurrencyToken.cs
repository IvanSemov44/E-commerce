namespace ECommerce.SharedKernel.Interfaces;

/// <summary>
/// Marker interface for entities that support optimistic concurrency via a row version token.
/// Implementing entities must apply [Timestamp] on the RowVersion property.
/// </summary>
public interface IConcurrencyToken
{
    byte[]? RowVersion { get; set; }
}
