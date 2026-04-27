namespace ECommerce.Contracts;

public record EmailVerifiedIntegrationEvent(
    Guid UserId,
    DateTime VerifiedAt) : IntegrationEvent;
