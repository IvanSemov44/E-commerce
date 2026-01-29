using ECommerce.Core.Entities;

namespace ECommerce.Application.Services;

/// <summary>
/// Service for sending emails through SendGrid.
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Send welcome email with email verification link.
    /// </summary>
    Task SendWelcomeEmailAsync(string email, string firstName, string verificationLink);

    /// <summary>
    /// Send email verification link.
    /// </summary>
    Task SendEmailVerificationAsync(string email, string firstName, string verificationLink);

    /// <summary>
    /// Send password reset email with reset link.
    /// </summary>
    Task SendPasswordResetEmailAsync(string email, string firstName, string resetLink);

    /// <summary>
    /// Send order confirmation email.
    /// </summary>
    Task SendOrderConfirmationEmailAsync(string email, Order order);

    /// <summary>
    /// Send order shipped notification with tracking info.
    /// </summary>
    Task SendOrderShippedEmailAsync(string email, Order order, string trackingNumber);

    /// <summary>
    /// Send order delivered confirmation.
    /// </summary>
    Task SendOrderDeliveredEmailAsync(string email, Order order);

    /// <summary>
    /// Send abandoned cart recovery email.
    /// </summary>
    Task SendAbandonedCartEmailAsync(string email, string firstName, Cart cart);

    /// <summary>
    /// Send low stock alert to admin.
    /// </summary>
    Task SendLowStockAlertAsync(string email, string firstName, string productName, int currentStock, int threshold, string? sku = null);

    /// <summary>
    /// Send marketing/newsletter email.
    /// </summary>
    Task SendMarketingEmailAsync(string email, string firstName, string subject, string htmlContent);
}
