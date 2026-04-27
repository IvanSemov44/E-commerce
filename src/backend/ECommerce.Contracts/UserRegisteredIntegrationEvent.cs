namespace ECommerce.Contracts;

public record UserRegisteredIntegrationEvent(
    Guid UserId,
    string Email,
    DateTime RegisteredAt) : IntegrationEvent;
