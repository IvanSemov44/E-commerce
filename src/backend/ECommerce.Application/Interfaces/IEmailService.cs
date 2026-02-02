using ECommerce.Core.Entities;

namespace ECommerce.Application.Interfaces;

/// <summary>
/// Service interface for sending emails through email providers (SendGrid/SMTP).
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Send welcome email with email verification link.
    /// </summary>
    Task SendWelcomeEmailAsync(string email, string firstName, string verificationLink, CancellationToken cancellationToken = default);

    /// <summary>
    /// Send email verification link.
    /// </summary>
    Task SendEmailVerificationAsync(string email, string firstName, string verificationLink, CancellationToken cancellationToken = default);

    /// <summary>
    /// Send password reset email with reset link.
    /// </summary>
    Task SendPasswordResetEmailAsync(string email, string firstName, string resetLink, CancellationToken cancellationToken = default);

    /// <summary>
    /// Send order confirmation email.
    /// </summary>
    Task SendOrderConfirmationEmailAsync(string email, Order order, CancellationToken cancellationToken = default);

    /// <summary>
    /// Send order shipped notification with tracking info.
    /// </summary>
    Task SendOrderShippedEmailAsync(string email, Order order, string trackingNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Send order delivered confirmation.
    /// </summary>
    Task SendOrderDeliveredEmailAsync(string email, Order order, CancellationToken cancellationToken = default);

    /// <summary>
    /// Send abandoned cart recovery email.
    /// </summary>
    Task SendAbandonedCartEmailAsync(string email, string firstName, Cart cart, CancellationToken cancellationToken = default);

    /// <summary>
    /// Send low stock alert to admin.
    /// </summary>
    Task SendLowStockAlertAsync(string email, string firstName, string productName, int currentStock, int threshold, string? sku = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Send marketing/newsletter email.
    /// </summary>
    Task SendMarketingEmailAsync(string email, string firstName, string subject, string htmlContent, CancellationToken cancellationToken = default);
}
