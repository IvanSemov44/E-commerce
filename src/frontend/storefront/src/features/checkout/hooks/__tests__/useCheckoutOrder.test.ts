import { describe, it, expect, vi, beforeEach } from 'vitest';
import { act } from '@testing-library/react';
import { renderHookWithProviders } from '@/shared/lib/test/test-utils';
import * as ordersApi from '@/features/orders/api';
import * as cartApi from '@/features/cart/api';
import * as checkoutApi from '@/features/checkout/api';
import { useCheckoutOrder } from '../useCheckoutOrder';
import type { ShippingFormData } from '@/features/checkout/types';

vi.mock('@/features/orders/api', () => ({
  useCreateOrderMutation: vi.fn(() => [vi.fn()]),
}));

vi.mock('@/features/cart/api', () => ({
  useClearCartMutation: vi.fn(() => [vi.fn()]),
}));

vi.mock('@/features/checkout/api', () => ({
  useCheckAvailabilityMutation: vi.fn(() => [vi.fn()]),
}));

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

describe('useCheckoutOrder', () => {
  let mockCreateOrder: ReturnType<typeof vi.fn>;
  let mockClearCart: ReturnType<typeof vi.fn>;
  let mockCheckAvailability: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    vi.clearAllMocks();

    mockCheckAvailability = vi.fn().mockReturnValue({
      unwrap: () => Promise.resolve({ isAvailable: true, issues: [] }),
    });
    mockCreateOrder = vi.fn().mockReturnValue({
      unwrap: () => Promise.resolve({ orderNumber: 'ORD-001' }),
    });
    mockClearCart = vi.fn().mockReturnValue({
      unwrap: () => Promise.resolve(),
    });

    vi.mocked(ordersApi.useCreateOrderMutation).mockReturnValue([mockCreateOrder] as never);
    vi.mocked(cartApi.useClearCartMutation).mockReturnValue([mockClearCart] as never);
    vi.mocked(checkoutApi.useCheckAvailabilityMutation).mockReturnValue([
      mockCheckAvailability,
    ] as never);
  });

  it('initialises with orderComplete false and no error', () => {
    const { result } = renderHookWithProviders(() => useCheckoutOrder(defaultOptions));

    expect(result.current.orderComplete).toBe(false);
    expect(result.current.error).toBeNull();
    expect(result.current.orderNumber).toBe('');
  });

  it('sets error when stock check fails', async () => {
    mockCheckAvailability.mockReturnValue({
      unwrap: () => Promise.reject(new Error('network')),
    });

    const { result } = renderHookWithProviders(() => useCheckoutOrder(defaultOptions));

    await act(async () => {
      await result.current.handleFormSubmit(mockShippingData);
    });

    expect(result.current.error).toBe('checkout.stockCheckFailed');
    expect(result.current.orderComplete).toBe(false);
  });

  it('sets error when stock is unavailable', async () => {
    mockCheckAvailability.mockReturnValue({
      unwrap: () =>
        Promise.resolve({
          isAvailable: false,
          issues: [{ productName: 'Widget', message: 'Out of stock' }],
        }),
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
    mockCreateOrder.mockReturnValue({
      unwrap: () => Promise.reject({ data: { message: 'Payment failed' } }),
    });

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

    expect(mockCreateOrder).toHaveBeenCalledWith(expect.objectContaining({ promoCode: 'SAVE10' }));
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

    expect(mockCreateOrder).toHaveBeenCalledWith(expect.objectContaining({ promoCode: undefined }));
  });

  it('falls back to errorObj.message when data.message is absent', async () => {
    mockCreateOrder.mockReturnValue({
      unwrap: () => Promise.reject({ message: 'Network error' }),
    });

    const { result } = renderHookWithProviders(() => useCheckoutOrder(defaultOptions));

    await act(async () => {
      await result.current.handleFormSubmit(mockShippingData);
    });

    expect(result.current.error).toBe('Network error');
  });
});
