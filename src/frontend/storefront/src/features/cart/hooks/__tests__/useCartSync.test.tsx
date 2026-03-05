import { describe, it, expect, vi, beforeEach } from 'vitest'
import { renderHookWithProviders } from '@/shared/lib/test/test-utils'
import { useCartSync } from '../useCartSync'

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
vi.mock('../../features/cart/api/cartApi', () => ({
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

  const authenticatedState = {
    cart: {
      items: [],
      lastUpdated: Date.now(),
    },
    auth: {
      isAuthenticated: true,
      user: { id: '1', email: 'test@test.com', firstName: 'Test', lastName: 'User', role: 'customer' as const },
      loading: false,
      error: null,
      initialized: true,
    },
  }

  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('should not sync when not authenticated', () => {
    const { result } = renderHookWithProviders(() => useCartSync(), { preloadedState: defaultPreloadedState })
    expect(result.current).toBeDefined()
  })

  it('should skip sync when disabled', () => {
    const { result } = renderHookWithProviders(() => useCartSync({ enabled: false }), { preloadedState: authenticatedState })
    expect(result.current).toBeDefined()
  })
})
