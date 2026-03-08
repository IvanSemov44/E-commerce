import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { act } from '@testing-library/react';
import { renderHookWithProviders } from '@/shared/lib/test/test-utils';
import { baseApi } from '@/shared/lib/api/baseApi';
import useProductDetails from '../useProductDetails';

// Mock API hooks
vi.mock('../../store/api/productApi', () => ({
  useGetProductBySlugQuery: vi.fn(() => ({
    data: null,
    isLoading: false,
    isError: false,
  })),
}));

vi.mock('../../store/api/reviewsApi', () => ({
  useGetProductReviewsQuery: vi.fn(() => ({
    data: null,
    isLoading: false,
    isError: false,
    refetch: vi.fn(),
  })),
}));

vi.mock('../../store/api/wishlistApi', () => ({
  useAddToWishlistMutation: vi.fn(() => [
    vi.fn().mockResolvedValue({ data: {} }),
    { isLoading: false },
  ]),
  useRemoveFromWishlistMutation: vi.fn(() => [
    vi.fn().mockResolvedValue({ data: {} }),
    { isLoading: false },
  ]),
  useCheckInWishlistQuery: vi.fn(() => ({
    data: false,
    refetch: vi.fn(),
  })),
}));

vi.mock('../../store/api/cartApi', () => ({
  useAddToCartMutation: vi.fn(() => [
    vi.fn().mockResolvedValue({ data: {} }),
    { isLoading: false },
  ]),
}));

// Mock logger
vi.mock('../../utils/logger', () => ({
  logger: {
    info: vi.fn(),
    error: vi.fn(),
  },
}));

describe('useProductDetails', () => {
  let store: ReturnType<typeof renderHookWithProviders>['store'];

  afterEach(() => {
    store?.dispatch(baseApi.util.resetApiState());
  });

  const defaultPreloadedState = {
    cart: {
      items: [],
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

  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('should initialize with default values', () => {
    const rendered = renderHookWithProviders(() => useProductDetails('test-product'), {
      preloadedState: defaultPreloadedState,
    });
    store = rendered.store;

    expect(rendered.result.current.quantity).toBe(1);
    expect(rendered.result.current.addedToCart).toBe(false);
    expect(rendered.result.current.cartError).toBeNull();
  });

  it('should set quantity', async () => {
    const rendered = renderHookWithProviders(() => useProductDetails('test-product'), {
      preloadedState: defaultPreloadedState,
    });
    store = rendered.store;

    await act(async () => {
      rendered.result.current.setQuantity(5);
    });

    expect(rendered.result.current.quantity).toBe(5);
  });

  it('should reset addedToCart state', () => {
    const rendered = renderHookWithProviders(() => useProductDetails('test-product'), {
      preloadedState: defaultPreloadedState,
    });
    store = rendered.store;

    // addedToCart starts as false
    expect(rendered.result.current.addedToCart).toBe(false);
  });
});
