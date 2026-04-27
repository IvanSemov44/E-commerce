namespace ECommerce.Contracts;

public record PasswordChangedIntegrationEvent(
    Guid UserId,
    DateTime ChangedAt) : IntegrationEvent;
