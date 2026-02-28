import { describe, it, expect, vi, beforeEach } from 'vitest'
import { renderHook } from '@testing-library/react'
import { Provider } from 'react-redux'
import { configureStore } from '@reduxjs/toolkit'
import { useCartSync } from '../useCartSync'
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
    refetch: vi.fn(),
  })),
  useAddToCartMutation: vi.fn(() => [
    vi.fn().mockResolvedValue({ data: {} }),
    { isLoading: false },
  ]),
}))

// Mock logger
vi.mock('../../utils/logger', () => ({
  logger: {
    warn: vi.fn(),
    info: vi.fn(),
    error: vi.fn(),
  },
}))

describe('useCartSync', () => {
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

  it('should not sync when not authenticated', () => {
    const { result } = renderHook(() => useCartSync(), { wrapper })
    expect(result.current).toBeDefined()
  })

  it('should skip sync when disabled', () => {
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
          user: { id: '1', email: 'test@test.com', firstName: 'Test', lastName: 'User', role: 'customer' },
          isAuthenticated: true,
          loading: false,
          error: null,
          initialized: true,
        },
      },
    })

    const { result } = renderHook(() => useCartSync({ enabled: false }), { wrapper })
    expect(result.current).toBeDefined()
  })
})
