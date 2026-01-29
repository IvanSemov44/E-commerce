using ECommerce.Core.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace ECommerce.Application.Services;

/// <summary>
/// SendGrid email service implementation.
/// </summary>
public class SendGridEmailService : IEmailService
{
    private readonly ISendGridClient _client;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SendGridEmailService> _logger;
    private readonly string _fromEmail;
    private readonly string _fromName;

    public SendGridEmailService(
        IConfiguration configuration,
        ILogger<SendGridEmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;

        var apiKey = configuration["SendGrid:ApiKey"] ?? "disabled";
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("SendGrid API key not configured. Email sending will be disabled.");
            apiKey = "disabled";
        }

        _client = new SendGridClient(apiKey);
        _fromEmail = configuration["SendGrid:FromEmail"] ?? "noreply@ecommerce.com";
        _fromName = configuration["SendGrid:FromName"] ?? "E-Commerce Platform";
    }

    public async Task SendWelcomeEmailAsync(string email, string firstName, string verificationLink)
    {
        var subject = $"Welcome to {_fromName}!";
        var htmlContent = $@"
            <html>
            <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
                <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                    <h2 style='color: #16a34a;'>Welcome, {firstName}!</h2>
                    <p>Thank you for registering with {_fromName}. We're excited to have you on board!</p>
                    <p>To get started, please verify your email address by clicking the button below:</p>
                    <div style='text-align: center; margin: 30px 0;'>
                        <a href='{verificationLink}'
                           style='background-color: #16a34a; color: white; padding: 12px 30px;
                                  text-decoration: none; border-radius: 5px; display: inline-block;'>
                            Verify Email Address
                        </a>
                    </div>
                    <p>If the button doesn't work, copy and paste this link into your browser:</p>
                    <p style='color: #666; word-break: break-all;'>{verificationLink}</p>
                    <p>This link will expire in 24 hours.</p>
                    <hr style='border: none; border-top: 1px solid #eee; margin: 30px 0;'>
                    <p style='color: #999; font-size: 12px;'>
                        If you didn't create an account with us, please ignore this email.
                    </p>
                </div>
            </body>
            </html>";

        await SendEmailAsync(email, subject, htmlContent);
    }

    public async Task SendEmailVerificationAsync(string email, string firstName, string verificationLink)
    {
        var subject = "Verify Your Email Address";
        var htmlContent = $@"
            <html>
            <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
                <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                    <h2 style='color: #16a34a;'>Email Verification</h2>
                    <p>Hi {firstName},</p>
                    <p>Please verify your email address by clicking the button below:</p>
                    <div style='text-align: center; margin: 30px 0;'>
                        <a href='{verificationLink}'
                           style='background-color: #16a34a; color: white; padding: 12px 30px;
                                  text-decoration: none; border-radius: 5px; display: inline-block;'>
                            Verify Email
                        </a>
                    </div>
                    <p>Or copy and paste this link:</p>
                    <p style='color: #666; word-break: break-all;'>{verificationLink}</p>
                    <p>This link expires in 24 hours.</p>
                </div>
            </body>
            </html>";

        await SendEmailAsync(email, subject, htmlContent);
    }

    public async Task SendPasswordResetEmailAsync(string email, string firstName, string resetLink)
    {
        var subject = "Reset Your Password";
        var htmlContent = $@"
            <html>
            <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
                <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                    <h2 style='color: #dc2626;'>Password Reset Request</h2>
                    <p>Hi {firstName},</p>
                    <p>We received a request to reset your password. Click the button below to create a new password:</p>
                    <div style='text-align: center; margin: 30px 0;'>
                        <a href='{resetLink}'
                           style='background-color: #dc2626; color: white; padding: 12px 30px;
                                  text-decoration: none; border-radius: 5px; display: inline-block;'>
                            Reset Password
                        </a>
                    </div>
                    <p>Or copy and paste this link:</p>
                    <p style='color: #666; word-break: break-all;'>{resetLink}</p>
                    <p><strong>This link expires in 1 hour.</strong></p>
                    <hr style='border: none; border-top: 1px solid #eee; margin: 30px 0;'>
                    <p style='color: #999; font-size: 12px;'>
                        If you didn't request a password reset, please ignore this email and your password will remain unchanged.
                    </p>
                </div>
            </body>
            </html>";

        await SendEmailAsync(email, subject, htmlContent);
    }

    public async Task SendOrderConfirmationEmailAsync(string email, Order order)
    {
        var subject = $"Order Confirmation - #{order.OrderNumber}";

        var itemsHtml = string.Join("", order.Items?.Select(item => $@"
            <tr>
                <td style='padding: 10px; border-bottom: 1px solid #eee;'>{item.ProductName}</td>
                <td style='padding: 10px; border-bottom: 1px solid #eee; text-align: center;'>{item.Quantity}</td>
                <td style='padding: 10px; border-bottom: 1px solid #eee; text-align: right;'>${item.UnitPrice:F2}</td>
                <td style='padding: 10px; border-bottom: 1px solid #eee; text-align: right;'>${item.TotalPrice:F2}</td>
            </tr>") ?? []);

        var htmlContent = $@"
            <html>
            <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
                <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                    <h2 style='color: #16a34a;'>Order Confirmed!</h2>
                    <p>Thank you for your order. We've received your order and will begin processing it shortly.</p>

                    <div style='background-color: #f3f4f6; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                        <p style='margin: 5px 0;'><strong>Order Number:</strong> {order.OrderNumber}</p>
                        <p style='margin: 5px 0;'><strong>Order Date:</strong> {order.CreatedAt:MMMM dd, yyyy}</p>
                        <p style='margin: 5px 0;'><strong>Status:</strong> {order.Status}</p>
                    </div>

                    <h3>Order Items</h3>
                    <table style='width: 100%; border-collapse: collapse;'>
                        <thead>
                            <tr style='background-color: #f9fafb;'>
                                <th style='padding: 10px; text-align: left; border-bottom: 2px solid #e5e7eb;'>Item</th>
                                <th style='padding: 10px; text-align: center; border-bottom: 2px solid #e5e7eb;'>Qty</th>
                                <th style='padding: 10px; text-align: right; border-bottom: 2px solid #e5e7eb;'>Price</th>
                                <th style='padding: 10px; text-align: right; border-bottom: 2px solid #e5e7eb;'>Total</th>
                            </tr>
                        </thead>
                        <tbody>
                            {itemsHtml}
                        </tbody>
                    </table>

                    <div style='margin-top: 20px; text-align: right;'>
                        <p style='margin: 5px 0;'>Subtotal: <strong>${order.Subtotal:F2}</strong></p>
                        {(order.DiscountAmount > 0 ? $"<p style='margin: 5px 0; color: #16a34a;'>Discount: <strong>-${order.DiscountAmount:F2}</strong></p>" : "")}
                        <p style='margin: 5px 0;'>Shipping: <strong>${order.ShippingAmount:F2}</strong></p>
                        <p style='margin: 5px 0;'>Tax: <strong>${order.TaxAmount:F2}</strong></p>
                        <p style='margin: 10px 0; font-size: 18px; color: #16a34a;'>
                            Total: <strong>${order.TotalAmount:F2}</strong>
                        </p>
                    </div>

                    <h3>Shipping Address</h3>
                    <div style='background-color: #f9fafb; padding: 15px; border-radius: 5px;'>
                        <p style='margin: 5px 0;'>{order.ShippingAddress?.FirstName ?? ""} {order.ShippingAddress?.LastName ?? ""}</p>
                        <p style='margin: 5px 0;'>{order.ShippingAddress?.StreetLine1 ?? ""}</p>
                        {(!string.IsNullOrEmpty(order.ShippingAddress?.StreetLine2) ? $"<p style='margin: 5px 0;'>{order.ShippingAddress?.StreetLine2}</p>" : "")}
                        <p style='margin: 5px 0;'>{order.ShippingAddress?.City ?? ""}, {order.ShippingAddress?.State ?? ""} {order.ShippingAddress?.PostalCode ?? ""}</p>
                        <p style='margin: 5px 0;'>{order.ShippingAddress?.Country ?? ""}</p>
                    </div>

                    <hr style='border: none; border-top: 1px solid #eee; margin: 30px 0;'>
                    <p style='color: #999; font-size: 12px;'>
                        You will receive another email when your order ships.
                    </p>
                </div>
            </body>
            </html>";

        await SendEmailAsync(email, subject, htmlContent);
    }

    public async Task SendOrderShippedEmailAsync(string email, Order order, string trackingNumber)
    {
        var subject = $"Your Order Has Shipped - #{order.OrderNumber}";
        var htmlContent = $@"
            <html>
            <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
                <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                    <h2 style='color: #16a34a;'>Your Order is on the Way!</h2>
                    <p>Great news! Your order has been shipped and is on its way to you.</p>

                    <div style='background-color: #f3f4f6; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                        <p style='margin: 5px 0;'><strong>Order Number:</strong> {order.OrderNumber}</p>
                        <p style='margin: 5px 0;'><strong>Tracking Number:</strong> {trackingNumber}</p>
                        <p style='margin: 5px 0;'><strong>Status:</strong> Shipped</p>
                    </div>

                    <div style='text-align: center; margin: 30px 0;'>
                        <a href='#'
                           style='background-color: #16a34a; color: white; padding: 12px 30px;
                                  text-decoration: none; border-radius: 5px; display: inline-block;'>
                            Track Your Package
                        </a>
                    </div>

                    <p>Estimated delivery: 3-5 business days</p>
                </div>
            </body>
            </html>";

        await SendEmailAsync(email, subject, htmlContent);
    }

    public async Task SendOrderDeliveredEmailAsync(string email, Order order)
    {
        var subject = $"Your Order Has Been Delivered - #{order.OrderNumber}";
        var htmlContent = $@"
            <html>
            <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
                <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                    <h2 style='color: #16a34a;'>Order Delivered!</h2>
                    <p>Your order has been successfully delivered. We hope you enjoy your purchase!</p>

                    <div style='background-color: #f3f4f6; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                        <p style='margin: 5px 0;'><strong>Order Number:</strong> {order.OrderNumber}</p>
                        <p style='margin: 5px 0;'><strong>Status:</strong> Delivered</p>
                    </div>

                    <p>We'd love to hear about your experience! Please take a moment to review your purchase.</p>

                    <div style='text-align: center; margin: 30px 0;'>
                        <a href='#'
                           style='background-color: #16a34a; color: white; padding: 12px 30px;
                                  text-decoration: none; border-radius: 5px; display: inline-block;'>
                            Leave a Review
                        </a>
                    </div>

                    <p>Thank you for shopping with us!</p>
                </div>
            </body>
            </html>";

        await SendEmailAsync(email, subject, htmlContent);
    }

    public async Task SendAbandonedCartEmailAsync(string email, string firstName, Cart cart)
    {
        var subject = "You Left Something Behind...";

        var itemsHtml = string.Join("", cart.Items?.Select(item => $@"
            <li style='margin: 10px 0;'>
                {item.Product?.Name ?? "Product"} - Qty: {item.Quantity} - ${(item.Product?.Price ?? 0) * item.Quantity:F2}
            </li>") ?? []);

        var totalAmount = cart.Items?.Sum(i => (i.Product?.Price ?? 0) * i.Quantity) ?? 0;

        var htmlContent = $@"
            <html>
            <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
                <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                    <h2 style='color: #16a34a;'>Don't Miss Out, {firstName}!</h2>
                    <p>You left some great items in your cart. They're waiting for you!</p>

                    <div style='background-color: #f9fafb; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                        <h3>Your Cart:</h3>
                        <ul style='list-style: none; padding: 0;'>
                            {itemsHtml}
                        </ul>
                        <p style='font-size: 18px; margin-top: 15px;'>
                            <strong>Total: ${totalAmount:F2}</strong>
                        </p>
                    </div>

                    <div style='text-align: center; margin: 30px 0;'>
                        <a href='#'
                           style='background-color: #16a34a; color: white; padding: 12px 30px;
                                  text-decoration: none; border-radius: 5px; display: inline-block;'>
                            Complete Your Purchase
                        </a>
                    </div>

                    <p>Items sell fast! Complete your order before they're gone.</p>
                </div>
            </body>
            </html>";

        await SendEmailAsync(email, subject, htmlContent);
    }

    public async Task SendLowStockAlertAsync(string email, string firstName, string productName, int currentStock, int threshold, string? sku = null)
    {
        var subject = $"Low Stock Alert - {productName}";
        var htmlContent = $@"
            <html>
            <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
                <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                    <h2 style='color: #dc2626;'>Low Stock Alert</h2>
                    <p>Hi {firstName},</p>
                    <p>The following product is running low on stock:</p>

                    <div style='background-color: #fef2f2; padding: 15px; border-radius: 5px; border-left: 4px solid #dc2626; margin: 20px 0;'>
                        <p style='margin: 5px 0;'><strong>Product:</strong> {productName}</p>
                        {(!string.IsNullOrWhiteSpace(sku) ? $"<p style='margin: 5px 0;'><strong>SKU:</strong> {sku}</p>" : "")}
                        <p style='margin: 5px 0;'><strong>Current Stock:</strong> {currentStock}</p>
                        <p style='margin: 5px 0;'><strong>Threshold:</strong> {threshold}</p>
                    </div>

                    <p>Please restock this product soon to avoid stockouts.</p>
                </div>
            </body>
            </html>";

        await SendEmailAsync(email, subject, htmlContent);
    }

    public async Task SendMarketingEmailAsync(string email, string firstName, string subject, string htmlContent)
    {
        var wrappedContent = $@"
            <html>
            <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
                <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                    <p>Hi {firstName},</p>
                    {htmlContent}
                    <hr style='border: none; border-top: 1px solid #eee; margin: 30px 0;'>
                    <p style='color: #999; font-size: 12px;'>
                        You're receiving this email because you subscribed to our newsletter.
                        <a href='#' style='color: #999;'>Unsubscribe</a>
                    </p>
                </div>
            </body>
            </html>";

        await SendEmailAsync(email, subject, wrappedContent);
    }

    /// <summary>
    /// Internal method to send email via SendGrid.
    /// </summary>
    private async Task SendEmailAsync(string toEmail, string subject, string htmlContent)
    {
        try
        {
            var from = new EmailAddress(_fromEmail, _fromName);
            var to = new EmailAddress(toEmail);
            var msg = MailHelper.CreateSingleEmail(from, to, subject, null, htmlContent);

            var response = await _client.SendEmailAsync(msg);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Email sent successfully to {Email} with subject: {Subject}", toEmail, subject);
            }
            else
            {
                var body = await response.Body.ReadAsStringAsync();
                _logger.LogError("Failed to send email to {Email}. Status: {StatusCode}, Response: {Response}",
                    toEmail, response.StatusCode, body);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while sending email to {Email} with subject: {Subject}", toEmail, subject);
        }
    }
}
