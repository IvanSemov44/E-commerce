namespace ECommerce.Application.Interfaces;

public interface IWebhookVerificationService
{
    bool VerifySignature(string payload, string signature);
}
