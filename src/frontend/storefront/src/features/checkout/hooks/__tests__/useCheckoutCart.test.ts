import { describe, it, expect, vi, beforeEach } from 'vitest';
import { renderHookWithProviders } from '@/shared/lib/test/test-utils';
import * as cartApi from '@/features/cart/api';
import * as cartHooks from '@/features/cart/hooks/useCartSync';
import { useCheckoutCart } from '../useCheckoutCart';

vi.mock('@/features/cart/api', () => ({
  useGetCartQuery: vi.fn(() => ({ data: undefined, isLoading: false })),
}));

vi.mock('@/features/cart/hooks/useCartSync', () => ({
  useCartSync: vi.fn(),
}));

describe('useCheckoutCart', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(cartApi.useGetCartQuery).mockReturnValue({
      data: undefined,
      isLoading: false,
    } as never);
    vi.mocked(cartHooks.useCartSync).mockReturnValue(undefined as never);
  });

  it('returns empty cart and zero subtotal for unauthenticated guest', () => {
    const { result } = renderHookWithProviders(() => useCheckoutCart(), {
      preloadedState: {
        auth: { isAuthenticated: false, user: null, token: null, refreshToken: null },
        cart: { items: [], totalItems: 0 },
      },
    });

    expect(result.current.cartItems).toEqual([]);
    expect(result.current.subtotal).toBe(0);
    expect(result.current.isLoading).toBe(false);
  });

  it('returns local cart items for guest users', () => {
    const localItems = [
      { id: 'p1', name: 'Widget', slug: 'widget', price: 10, quantity: 2, maxStock: 5, image: '' },
    ];

    const { result } = renderHookWithProviders(() => useCheckoutCart(), {
      preloadedState: {
        auth: { isAuthenticated: false, user: null, token: null, refreshToken: null },
        cart: { items: localItems, totalItems: 2 },
      },
    });

    expect(result.current.cartItems).toEqual(localItems);
    expect(result.current.subtotal).toBe(20);
  });

  it('returns isLoading true while backend cart is loading for authenticated user', () => {
    vi.mocked(cartApi.useGetCartQuery).mockReturnValue({
      data: undefined,
      isLoading: true,
    } as never);

    const { result } = renderHookWithProviders(() => useCheckoutCart(), {
      preloadedState: {
        auth: { isAuthenticated: true, user: { id: 'u1' }, token: 'tok', refreshToken: 'ref' },
        cart: { items: [], totalItems: 0 },
      },
    });

    expect(result.current.isLoading).toBe(true);
  });

  it('maps backend cart items for authenticated user', () => {
    const backendCart = {
      items: [
        {
          productId: 'p1',
          productName: 'Widget',
          price: 25,
          quantity: 1,
          productImage: 'img.jpg',
          imageUrl: '',
        },
      ],
    };
    vi.mocked(cartApi.useGetCartQuery).mockReturnValue({
      data: backendCart,
      isLoading: false,
    } as never);

    const { result } = renderHookWithProviders(() => useCheckoutCart(), {
      preloadedState: {
        auth: { isAuthenticated: true, user: { id: 'u1' }, token: 'tok', refreshToken: 'ref' },
        cart: { items: [], totalItems: 0 },
      },
    });

    expect(result.current.cartItems).toHaveLength(1);
    expect(result.current.cartItems[0].name).toBe('Widget');
    expect(result.current.subtotal).toBe(25);
    expect(result.current.isLoading).toBe(false);
  });

  it('uses imageUrl as fallback when productImage is empty', () => {
    vi.mocked(cartApi.useGetCartQuery).mockReturnValue({
      data: {
        items: [
          {
            productId: 'p1',
            productName: 'Widget',
            price: 10,
            quantity: 1,
            productImage: '',
            imageUrl: 'fallback.jpg',
          },
        ],
      },
      isLoading: false,
    } as never);

    const { result } = renderHookWithProviders(() => useCheckoutCart(), {
      preloadedState: {
        auth: { isAuthenticated: true, user: { id: 'u1' }, token: 'tok', refreshToken: 'ref' },
        cart: { items: [], totalItems: 0 },
      },
    });

    expect(result.current.cartItems[0].image).toBe('fallback.jpg');
  });

  it('falls back to local cart when authenticated but backend data is not yet available', () => {
    const localItems = [
      { id: 'p1', name: 'Widget', slug: 'widget', price: 10, quantity: 1, maxStock: 5, image: '' },
    ];

    const { result } = renderHookWithProviders(() => useCheckoutCart(), {
      preloadedState: {
        auth: { isAuthenticated: true, user: { id: 'u1' }, token: 'tok', refreshToken: 'ref' },
        cart: { items: localItems, totalItems: 1 },
      },
    });

    expect(result.current.cartItems).toEqual(localItems);
  });
});
