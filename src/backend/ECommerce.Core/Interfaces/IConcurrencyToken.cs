namespace ECommerce.Core.Interfaces;

/// <summary>
/// Interface for entities that support optimistic concurrency control.
/// Entities implementing this interface should have a RowVersion property
/// that is configured as a concurrency token in EF Core.
/// </summary>
/// <remarks>
/// <para>
/// Optimistic concurrency is recommended for entities that:
/// </para>
/// <list type="bullet">
///   <item><description>Are frequently updated by multiple users/processes simultaneously</description></item>
///   <item><description>Have critical data that shouldn't be overwritten (e.g., inventory counts, order status)</description></item>
///   <item><description>Require audit trails of concurrent modification attempts</description></item>
/// </list>
/// <para>
/// Current entities implementing this interface:
/// </para>
/// <list type="bullet">
///   <item><description>Product - for inventory and pricing updates</description></item>
///   <item><description>Order - for status and payment updates</description></item>
///   <item><description>PromoCode - for usage count updates</description></item>
/// </list>
/// <para>
/// NOTE: The [Timestamp] attribute must be applied on the implementing class property,
/// not on this interface. Attributes on interface properties are not inherited by
/// implementing classes in C#.
/// </para>
/// </remarks>
public interface IConcurrencyToken
{
    /// <summary>
    /// Gets or sets the row version for optimistic concurrency control.
    /// This property is automatically managed by EF Core and the database.
    /// </summary>
    /// <value>
    /// A byte array representing the version stamp. Null for new entities,
    /// populated by the database after first save.
    /// </value>
    byte[]? RowVersion { get; set; }
}
