using System.Collections.Concurrent;
using ECommerce.Application.DTOs.Payments;
using ECommerce.Application.Interfaces;

namespace ECommerce.Application.Services;

/// <summary>
/// In-memory payment store for development/testing.
/// Thread-safe using ConcurrentDictionary.
/// For production, replace with database-backed implementation.
/// </summary>
public class InMemoryPaymentStore : IPaymentStore
{
    private readonly ConcurrentDictionary<string, PaymentDetailsDto> _store = new();

    public Task StorePaymentAsync(string paymentId, PaymentDetailsDto details, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _store[paymentId] = details;
        return Task.CompletedTask;
    }

    public Task<PaymentDetailsDto?> GetPaymentAsync(string paymentId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _store.TryGetValue(paymentId, out var details);
        return Task.FromResult(details);
    }

    public Task RemovePaymentAsync(string paymentId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _store.TryRemove(paymentId, out _);
        return Task.CompletedTask;
    }
}
