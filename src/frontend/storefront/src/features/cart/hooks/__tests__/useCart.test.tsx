import { describe, it, expect, vi, afterEach } from 'vitest';
import { renderHookWithProviders } from '@/shared/lib/test/test-utils';
import { baseApi } from '@/shared/lib/api/baseApi';
import { useCart } from '../useCart';

// Mock API hooks
vi.mock('../../store/api/cartApi', () => ({
  useGetCartQuery: vi.fn(() => ({
    data: null,
    isLoading: false,
    isError: false,
  })),
  useUpdateCartItemMutation: vi.fn(() => [
    vi.fn().mockResolvedValue({ data: {} }),
    { isLoading: false },
  ]),
  useRemoveFromCartMutation: vi.fn(() => [
    vi.fn().mockResolvedValue({ data: {} }),
    { isLoading: false },
  ]),
}));

// Mock useCartSync hook
vi.mock('../useCartSync', () => ({
  useCartSync: vi.fn(() => ({
    backendCart: null,
    isLoading: false,
  })),
}));

describe('useCart', () => {
  let store: ReturnType<typeof renderHookWithProviders>['store'];

  afterEach(() => {
    store?.dispatch(baseApi.util.resetApiState());
  });

  const defaultPreloadedState = {
    cart: {
      items: [
        {
          id: '1',
          name: 'Test Product',
          slug: 'test-product',
          price: 29.99,
          quantity: 2,
          maxStock: 10,
          image: '/test.jpg',
        },
      ],
      lastUpdated: Date.now(),
    },
    auth: {
      isAuthenticated: false,
      user: null,
      loading: false,
      error: null,
      initialized: true,
    },
  };

  it('should calculate totals correctly', () => {
    const rendered = renderHookWithProviders(() => useCart(), {
      preloadedState: defaultPreloadedState,
    });
    store = rendered.store;

    expect(rendered.result.current.totals.subtotal).toBe(59.98); // 29.99 * 2
    expect(rendered.result.current.totals.shipping).toBeGreaterThan(0);
    expect(rendered.result.current.totals.tax).toBeGreaterThan(0);
    expect(rendered.result.current.totals.total).toBeGreaterThan(59.98);
  });

  it('should provide display items from local cart when not authenticated', () => {
    const rendered = renderHookWithProviders(() => useCart(), {
      preloadedState: defaultPreloadedState,
    });
    store = rendered.store;

    expect(rendered.result.current.displayItems).toHaveLength(1);
    expect(rendered.result.current.displayItems[0].id).toBe('1');
    expect(rendered.result.current.displayItems[0].name).toBe('Test Product');
    expect(rendered.result.current.isAuthenticated).toBe(false);
  });

  it('should calculate free shipping when threshold is met', () => {
    const highValueState = {
      cart: {
        items: [
          {
            id: '1',
            name: 'Expensive Product',
            slug: 'expensive-product',
            price: 150.0,
            quantity: 1,
            maxStock: 10,
            image: '/test.jpg',
          },
        ],
        lastUpdated: Date.now(),
      },
      auth: {
        user: null,
        isAuthenticated: false,
        loading: false,
        error: null,
        initialized: true,
      },
    };

    const rendered = renderHookWithProviders(() => useCart(), { preloadedState: highValueState });
    store = rendered.store;

    // Assuming FREE_SHIPPING_THRESHOLD is 100
    expect(rendered.result.current.totals.subtotal).toBe(150.0);
    expect(rendered.result.current.totals.shipping).toBe(0); // Free shipping
  });

  it('should have zero shipping for empty cart', () => {
    const emptyState = {
      cart: {
        items: [],
        lastUpdated: Date.now(),
      },
      auth: {
        user: null,
        isAuthenticated: false,
        loading: false,
        error: null,
        initialized: true,
      },
    };

    const rendered = renderHookWithProviders(() => useCart(), { preloadedState: emptyState });
    store = rendered.store;

    expect(rendered.result.current.totals.subtotal).toBe(0);
    expect(rendered.result.current.totals.shipping).toBe(0);
    expect(rendered.result.current.totals.tax).toBe(0);
    expect(rendered.result.current.totals.total).toBe(0);
  });
});
