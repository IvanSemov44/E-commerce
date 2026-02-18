import { describe, it, expect, vi, beforeEach } from 'vitest'
import { renderHook } from '@testing-library/react'
import { Provider } from 'react-redux'
import { configureStore } from '@reduxjs/toolkit'
import { useCart } from '../useCart'
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
}))

// Mock useCartSync hook
vi.mock('../useCartSync', () => ({
  useCartSync: vi.fn(() => ({
    backendCart: null,
    isLoading: false,
  })),
}))

describe('useCart', () => {
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
              price: 29.99,
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
  })

  it('should calculate totals correctly', () => {
    const { result } = renderHook(() => useCart(), { wrapper })

    expect(result.current.totals.subtotal).toBe(59.98) // 29.99 * 2
    expect(result.current.totals.shipping).toBeGreaterThan(0)
    expect(result.current.totals.tax).toBeGreaterThan(0)
    expect(result.current.totals.total).toBeGreaterThan(59.98)
  })

  it('should provide display items from local cart when not authenticated', () => {
    const { result } = renderHook(() => useCart(), { wrapper })

    expect(result.current.displayItems).toHaveLength(1)
    expect(result.current.displayItems[0].id).toBe('1')
    expect(result.current.displayItems[0].name).toBe('Test Product')
    expect(result.current.isAuthenticated).toBe(false)
  })

  it('should calculate free shipping when threshold is met', () => {
    // Create store with high-value items to meet free shipping threshold
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
              name: 'Expensive Product',
              slug: 'expensive-product',
              price: 150.00,
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
      },
    })

    const { result } = renderHook(() => useCart(), { wrapper })

    // Assuming FREE_SHIPPING_THRESHOLD is 100
    expect(result.current.totals.subtotal).toBe(150.00)
    expect(result.current.totals.shipping).toBe(0) // Free shipping
  })

  it('should have zero shipping for empty cart', () => {
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

    const { result } = renderHook(() => useCart(), { wrapper })

    expect(result.current.totals.subtotal).toBe(0)
    expect(result.current.totals.shipping).toBe(0)
    expect(result.current.totals.tax).toBe(0)
    expect(result.current.totals.total).toBe(0)
  })

  it('should provide update and remove handlers', () => {
    const { result } = renderHook(() => useCart(), { wrapper })

    expect(typeof result.current.handleUpdateQuantity).toBe('function')
    expect(typeof result.current.handleRemove).toBe('function')
  })
})
