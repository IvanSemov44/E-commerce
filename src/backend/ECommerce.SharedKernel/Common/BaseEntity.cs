namespace ECommerce.SharedKernel.Common;

/// <summary>
/// Base class for all entities in the domain.
/// Provides common properties for auditing and identification.
/// </summary>
/// <remarks>
/// Entities that require optimistic concurrency control should implement
/// <see cref="Interfaces.IConcurrencyToken"/> interface separately.
/// This keeps the base class clean and only adds RowVersion where needed.
/// </remarks>
public abstract class BaseEntity
{
    /// <summary>
    /// Gets or sets the unique identifier for the entity.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the timestamp when the entity was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the timestamp when the entity was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
