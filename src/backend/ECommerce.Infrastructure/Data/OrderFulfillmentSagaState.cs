namespace ECommerce.Infrastructure.Data;

/// <summary>
/// Persisted saga state for the minimal order fulfillment orchestration flow.
/// </summary>
public class OrderFulfillmentSagaState
{
    public Guid Id { get; set; }

    public Guid CorrelationId { get; set; }

    public Guid OrderId { get; set; }

    public Guid CustomerId { get; set; }

    public string CurrentState { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public string? FailureReason { get; set; }
}
