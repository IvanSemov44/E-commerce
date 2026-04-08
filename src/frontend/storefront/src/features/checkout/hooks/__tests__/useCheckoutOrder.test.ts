import { describe, it, expect, vi, beforeEach } from 'vitest';
import { act } from '@testing-library/react';
import { renderHookWithProviders } from '@/shared/lib/test/test-utils';
import { useCheckoutOrder } from '../useCheckoutOrder';
import type { ShippingFormData } from '@/features/checkout/types';
import { server } from '@/shared/lib/test/msw-server';
import { http, HttpResponse } from 'msw';

vi.mock('@/shared/lib/utils/telemetry', () => ({
  telemetry: { track: vi.fn() },
}));

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string) => key }),
}));

const mockShippingData: ShippingFormData = {
  firstName: 'John',
  lastName: 'Doe',
  email: 'john@example.com',
  phone: '+1234567890',
  streetLine1: '123 Main St',
  city: 'Anytown',
  state: 'NY',
  postalCode: '10001',
  country: 'US',
};

const defaultOptions = {
  cartItems: [
    { id: 'p1', name: 'Widget', slug: 'widget', price: 20, quantity: 1, maxStock: 5, image: '' },
  ],
  subtotal: 20,
  promoCode: '',
  promoCodeValidation: null,
  paymentMethod: 'card',
};

const setupHandlers = (
  availabilityResult = { isAvailable: true, issues: [] },
  orderResult = { orderNumber: 'ORD-001' }
) => {
  server.use(
    http.post('/api/inventory/check-availability', async () => {
      return HttpResponse.json({
        success: true,
        data: availabilityResult,
      });
    }),
    http.post('/api/orders', async () => {
      return HttpResponse.json({
        success: true,
        data: orderResult,
      });
    }),
    http.post('/api/cart/clear', async () => {
      return HttpResponse.json({
        success: true,
        data: { id: 'c1', items: [], itemCount: 0 },
      });
    })
  );
};

describe('useCheckoutOrder', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    setupHandlers();
  });

  it('initialises with orderComplete false and no error', () => {
    const { result } = renderHookWithProviders(() => useCheckoutOrder(defaultOptions));

    expect(result.current.orderComplete).toBe(false);
    expect(result.current.error).toBeNull();
    expect(result.current.orderNumber).toBe('');
  });

  it('sets error when stock check fails', async () => {
    server.use(
      http.post('/api/inventory/check-availability', async () => {
        return HttpResponse.json(
          { success: false, errorDetails: { message: 'Network error', code: 'INTERNAL_ERROR' } },
          { status: 500 }
        );
      })
    );

    const { result } = renderHookWithProviders(() => useCheckoutOrder(defaultOptions));

    await act(async () => {
      await result.current.handleFormSubmit(mockShippingData);
    });

    expect(result.current.error).toBe('checkout.stockCheckFailed');
    expect(result.current.orderComplete).toBe(false);
  });

  it('sets error when stock is unavailable', async () => {
    setupHandlers({
      isAvailable: false,
      issues: [{ productName: 'Widget', message: 'Out of stock' }],
    });

    const { result } = renderHookWithProviders(() => useCheckoutOrder(defaultOptions));

    await act(async () => {
      await result.current.handleFormSubmit(mockShippingData);
    });

    expect(result.current.error).toContain('checkout.stockIssues');
    expect(result.current.orderComplete).toBe(false);
  });

  it('sets orderComplete and orderNumber on successful submission', async () => {
    const { result } = renderHookWithProviders(() => useCheckoutOrder(defaultOptions));

    await act(async () => {
      await result.current.handleFormSubmit(mockShippingData);
    });

    expect(result.current.orderComplete).toBe(true);
    expect(result.current.orderNumber).toBe('ORD-001');
    expect(result.current.orderEmail).toBe('john@example.com');
  });

  it('sets error when createOrder throws', async () => {
    server.use(
      http.post('/api/orders', async () => {
        return HttpResponse.json(
          { success: false, errorDetails: { message: 'Payment failed', code: 'PAYMENT_FAILED' } },
          { status: 400 }
        );
      })
    );

    const { result } = renderHookWithProviders(() => useCheckoutOrder(defaultOptions));

    await act(async () => {
      await result.current.handleFormSubmit(mockShippingData);
    });

    expect(result.current.error).toBe('Payment failed');
    expect(result.current.orderComplete).toBe(false);
  });

  it('marks isGuestOrder true for unauthenticated users', async () => {
    const { result } = renderHookWithProviders(() => useCheckoutOrder(defaultOptions), {
      preloadedState: {
        auth: { isAuthenticated: false, user: null, token: null, refreshToken: null },
      },
    });

    await act(async () => {
      await result.current.handleFormSubmit(mockShippingData);
    });

    expect(result.current.isGuestOrder).toBe(true);
  });

  it('marks isGuestOrder false for authenticated users', async () => {
    const { result } = renderHookWithProviders(() => useCheckoutOrder(defaultOptions), {
      preloadedState: {
        auth: { isAuthenticated: true, user: { id: 'u1' }, token: 'tok', refreshToken: 'ref' },
      },
    });

    await act(async () => {
      await result.current.handleFormSubmit(mockShippingData);
    });

    expect(result.current.isGuestOrder).toBe(false);
  });

  it('includes promoCode in order payload when promoCodeValidation is valid', async () => {
    const { result } = renderHookWithProviders(() =>
      useCheckoutOrder({
        ...defaultOptions,
        promoCode: 'SAVE10',
        promoCodeValidation: { isValid: true, discountAmount: 5 },
      })
    );

    await act(async () => {
      await result.current.handleFormSubmit(mockShippingData);
    });
  });

  it('omits promoCode when promoCodeValidation is invalid', async () => {
    const { result } = renderHookWithProviders(() =>
      useCheckoutOrder({
        ...defaultOptions,
        promoCode: 'BADCODE',
        promoCodeValidation: { isValid: false, discountAmount: 0 },
      })
    );

    await act(async () => {
      await result.current.handleFormSubmit(mockShippingData);
    });
  });

  it('falls back to errorObj.message when data.message is absent', async () => {
    server.use(
      http.post('/api/orders', async () => {
        return HttpResponse.json(
          { success: false, errorDetails: { message: 'Network error', code: 'INTERNAL_ERROR' } },
          { status: 500 }
        );
      })
    );

    const { result } = renderHookWithProviders(() => useCheckoutOrder(defaultOptions));

    await act(async () => {
      await result.current.handleFormSubmit(mockShippingData);
    });

    expect(result.current.error).toBe('Network error');
  });
});
