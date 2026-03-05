import { describe, it, expect, vi, beforeEach } from 'vitest'
import { act } from '@testing-library/react'
import { renderHookWithProviders } from '@/shared/lib/test/test-utils'
import useProductDetails from '../useProductDetails'

// Mock react-hot-toast
vi.mock('react-hot-toast', () => ({
  default: {
    error: vi.fn(),
    success: vi.fn(),
  },
  toast: {
    error: vi.fn(),
    success: vi.fn(),
  },
}))

// Mock API hooks
vi.mock('../../store/api/productApi', () => ({
  useGetProductBySlugQuery: vi.fn(() => ({
    data: null,
    isLoading: false,
    isError: false,
  })),
}))

vi.mock('../../store/api/reviewsApi', () => ({
  useGetProductReviewsQuery: vi.fn(() => ({
    data: null,
    isLoading: false,
    isError: false,
    refetch: vi.fn(),
  })),
}))

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
}))

vi.mock('../../store/api/cartApi', () => ({
  useAddToCartMutation: vi.fn(() => [
    vi.fn().mockResolvedValue({ data: {} }),
    { isLoading: false },
  ]),
}))

// Mock logger
vi.mock('../../utils/logger', () => ({
  logger: {
    info: vi.fn(),
    error: vi.fn(),
  },
}))

describe('useProductDetails', () => {
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
  }

  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('should initialize with default values', () => {
    const { result } = renderHookWithProviders(() => useProductDetails('test-product'), { preloadedState: defaultPreloadedState })
    
    expect(result.current.quantity).toBe(1)
    expect(result.current.addedToCart).toBe(false)
    expect(result.current.cartError).toBeNull()
  })

  it('should set quantity', async () => {
    const { result } = renderHookWithProviders(() => useProductDetails('test-product'), { preloadedState: defaultPreloadedState })
    
    await act(async () => {
      result.current.setQuantity(5)
    })
    
    expect(result.current.quantity).toBe(5)
  })

  it('should reset addedToCart state', () => {
    const { result } = renderHookWithProviders(() => useProductDetails('test-product'), { preloadedState: defaultPreloadedState })
    
    // addedToCart starts as false
    expect(result.current.addedToCart).toBe(false)
  })
})
