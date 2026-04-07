using System.Collections.Concurrent;
using ECommerce.Payments.Application.DTOs;
using ECommerce.Payments.Application.Interfaces;

namespace ECommerce.Payments.Infrastructure.Services;

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
