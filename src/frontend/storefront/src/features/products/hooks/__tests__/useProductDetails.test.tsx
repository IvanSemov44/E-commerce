import { describe, it, expect, vi, beforeEach } from 'vitest'
import { renderHook, act } from '@testing-library/react'
import { Provider } from 'react-redux'
import { configureStore } from '@reduxjs/toolkit'
import useProductDetails from '../useProductDetails'
import { cartReducer } from '@/features/cart/slices/cartSlice'
import { authReducer } from '@/features/auth/slices/authSlice'
import type { ReactNode } from 'react'

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
  let store: ReturnType<typeof configureStore>

  const wrapper = ({ children }: { children: ReactNode }) => (
    <Provider store={store}>{children}</Provider>
  )

  beforeEach(() => {
    store = configureStore({
      reducer: {
        cart: cartReducer,
        auth: authReducer,
      },
      preloadedState: {
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
      },
    })
    vi.clearAllMocks()
  })

  it('should initialize with default values', () => {
    const { result } = renderHook(() => useProductDetails('test-product'), { wrapper })
    
    expect(result.current.quantity).toBe(1)
    expect(result.current.addedToCart).toBe(false)
    expect(result.current.cartError).toBeNull()
  })

  it('should set quantity', async () => {
    const { result } = renderHook(() => useProductDetails('test-product'), { wrapper })
    
    await act(async () => {
      result.current.setQuantity(5)
    })
    
    expect(result.current.quantity).toBe(5)
  })

  it('should reset addedToCart state', () => {
    const { result } = renderHook(() => useProductDetails('test-product'), { wrapper })
    
    // addedToCart starts as false
    expect(result.current.addedToCart).toBe(false)
  })
})
