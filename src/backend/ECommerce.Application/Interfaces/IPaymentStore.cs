using ECommerce.Application.DTOs.Payments;

namespace ECommerce.Application.Interfaces;

/// <summary>
/// Interface for storing and retrieving payment details.
/// Use in-memory implementation for development, database-backed for production.
/// </summary>
public interface IPaymentStore
{
    /// <summary>
    /// Stores payment details asynchronously.
    /// </summary>
    /// <param name="paymentId">The payment intent ID.</param>
    /// <param name="details">The payment details to store.</param>
    Task StorePaymentAsync(string paymentId, PaymentDetailsDto details);

    /// <summary>
    /// Retrieves payment details by payment ID asynchronously.
    /// </summary>
    /// <param name="paymentId">The payment intent ID.</param>
    /// <returns>The payment details if found; otherwise null.</returns>
    Task<PaymentDetailsDto?> GetPaymentAsync(string paymentId);

    /// <summary>
    /// Removes payment details from the store asynchronously.
    /// </summary>
    /// <param name="paymentId">The payment intent ID.</param>
    Task RemovePaymentAsync(string paymentId);
}
