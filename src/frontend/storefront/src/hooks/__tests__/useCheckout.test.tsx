import { describe, it, expect, vi, beforeEach } from 'vitest'
import { renderHook } from '@testing-library/react'
import { Provider } from 'react-redux'
import { configureStore } from '@reduxjs/toolkit'
import { useCheckout } from '../useCheckout'
import { cartReducer } from '../../store/slices/cartSlice'
import { authReducer } from '../../store/slices/authSlice'
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
vi.mock('../../store/api/ordersApi', () => ({
  useCreateOrderMutation: vi.fn(() => [
    vi.fn().mockResolvedValue({ orderNumber: 'ORD-123' }),
    { isLoading: false },
  ]),
}))

vi.mock('../../store/api/cartApi', () => ({
  useGetCartQuery: vi.fn(() => ({
    data: null,
    isLoading: false,
  })),
  useClearCartMutation: vi.fn(() => [
    vi.fn().mockResolvedValue({}),
    { isLoading: false },
  ]),
}))

vi.mock('../../store/api/promoCodeApi', () => ({
  useValidatePromoCodeMutation: vi.fn(() => [
    vi.fn().mockResolvedValue({ isValid: true, discountAmount: 10 }),
    { isLoading: false },
  ]),
}))

vi.mock('../../store/api/inventoryApi', () => ({
  useCheckAvailabilityMutation: vi.fn(() => [
    vi.fn().mockResolvedValue({ isAvailable: true, issues: [] }),
    { isLoading: false },
  ]),
}))

// Mock useCartSync
vi.mock('../useCartSync', () => ({
  useCartSync: vi.fn(() => ({})),
}))

// Mock constants
vi.mock('../../utils/constants', () => ({
  FREE_SHIPPING_THRESHOLD: 100,
  STANDARD_SHIPPING_COST: 10,
  DEFAULT_TAX_RATE: 0.08,
}))

// Mock validators
vi.mock('../../utils/validation', () => ({
  validators: {
    required: () => () => null,
    email: () => () => null,
    phone: () => () => null,
  },
}))

describe('useCheckout', () => {
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
          items: [
            {
              id: '1',
              name: 'Test Product',
              slug: 'test-product',
              price: 50,
              quantity: 2,
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
      },
    })
    vi.clearAllMocks()
  })

  it('should initialize with default values', () => {
    const { result } = renderHook(() => useCheckout(), { wrapper })

    expect(result.current.promoCode).toBe('')
    expect(result.current.orderComplete).toBe(false)
    expect(result.current.error).toBeNull()
    expect(result.current.isGuestOrder).toBe(false)
  })

  it('should set promo code', () => {
    const { result } = renderHook(() => useCheckout(), { wrapper })

    result.current.setPromoCode('SAVE10')

    expect(result.current.promoCode).toBe('SAVE10')
  })

  it('should have cart items from local state', () => {
    const { result } = renderHook(() => useCheckout(), { wrapper })

    expect(result.current.cartItems.length).toBe(1)
    expect(result.current.cartItems[0].name).toBe('Test Product')
  })

  it('should calculate subtotal', () => {
    const { result } = renderHook(() => useCheckout(), { wrapper })

    expect(result.current.subtotal).toBe(100) // 50 * 2
  })

  it('should have handleSubmit function', () => {
    const { result } = renderHook(() => useCheckout(), { wrapper })

    expect(typeof result.current.handleSubmit).toBe('function')
  })

  it('should have setFormData function', () => {
    const { result } = renderHook(() => useCheckout(), { wrapper })

    expect(typeof result.current.setFormData).toBe('function')
  })

  it('should calculate totals', () => {
    const { result } = renderHook(() => useCheckout(), { wrapper })

    // Free shipping since subtotal is 100
    expect(result.current.shipping).toBe(0)
    // Tax is 8% of 100 = 8
    expect(result.current.tax).toBe(8)
  })
})
