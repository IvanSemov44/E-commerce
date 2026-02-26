import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen } from '@testing-library/react'
import { BrowserRouter } from 'react-router-dom'
import { Provider } from 'react-redux'
import { configureStore } from '@reduxjs/toolkit'
import Cart from '../Cart'
import { authSlice } from '../../store/slices/authSlice'
import { cartReducer } from '../../store/slices/cartSlice'
import toastReducer from '../../store/slices/toastSlice'

// Mock the useCart hook
vi.mock('../../hooks/useCart', () => ({
  useCart: vi.fn(),
}))

// Mock useCartSync hook
vi.mock('../../hooks/useCartSync', () => ({
  useCartSync: vi.fn(() => ({ isLoading: false })),
}))

// Mock the RTK Query hooks
vi.mock('../../store/api/cartApi', () => ({
  useGetCartQuery: vi.fn(() => ({
    data: { items: [], total: 0 },
    isLoading: false,
    error: null,
    refetch: vi.fn(),
  })),
  useUpdateCartItemMutation: vi.fn(() => [vi.fn(), { isLoading: false }]),
  useRemoveFromCartMutation: vi.fn(() => [vi.fn(), { isLoading: false }]),
  useClearCartMutation: vi.fn(() => [vi.fn(), { isLoading: false }]),
}))

// Mock useNavigate
const mockNavigate = vi.fn()
vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual('react-router-dom')
  return {
    ...actual,
    useNavigate: () => mockNavigate,
  }
})

import { useCart } from '../../hooks/useCart'

const mockCartItems = [
  {
    id: '1',
    productId: '1',
    name: 'Test Product 1',
    price: 29.99,
    quantity: 2,
    image: 'https://example.com/image1.jpg',
    slug: 'test-product-1',
    maxStock: 10,
  },
  {
    id: '2',
    productId: '2',
    name: 'Test Product 2',
    price: 49.99,
    quantity: 1,
    image: 'https://example.com/image2.jpg',
    slug: 'test-product-2',
    maxStock: 10,
  },
]

const createMockStore = () => {
  return configureStore({
    reducer: {
      cart: cartReducer,
      auth: authSlice.reducer,
      toast: toastReducer,
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
}

const renderCart = () => {
  const store = createMockStore()
  return render(
    <Provider store={store}>
      <BrowserRouter>
        <Cart />
      </BrowserRouter>
    </Provider>
  )
}

describe('Cart Page', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  describe('Empty Cart', () => {
    it('shows empty cart message when cart is empty', () => {
      vi.mocked(useCart).mockReturnValue({
        displayItems: [],
        totals: { subtotal: 0, shipping: 0, tax: 0, total: 0 },
        isLoading: false,
        isAuthenticated: false,
        handleUpdateQuantity: vi.fn(),
        handleRemove: vi.fn(),
      })
      renderCart()
      expect(screen.getByText(/your cart is empty/i)).toBeInTheDocument()
    })

    it('shows continue shopping button when cart is empty', () => {
      vi.mocked(useCart).mockReturnValue({
        displayItems: [],
        totals: { subtotal: 0, shipping: 0, tax: 0, total: 0 },
        isLoading: false,
        isAuthenticated: false,
        handleUpdateQuantity: vi.fn(),
        handleRemove: vi.fn(),
      })
      renderCart()
      expect(screen.getByRole('button', { name: /continue shopping/i })).toBeInTheDocument()
    })
  })

  describe('Cart with Items', () => {
    it('displays cart items', () => {
      vi.mocked(useCart).mockReturnValue({
        displayItems: mockCartItems,
        totals: { subtotal: 109.97, shipping: 0, tax: 0, total: 109.97 },
        isLoading: false,
        isAuthenticated: false,
        handleUpdateQuantity: vi.fn(),
        handleRemove: vi.fn(),
      })
      renderCart()
      expect(screen.getByText('Test Product 1')).toBeInTheDocument()
      expect(screen.getByText('Test Product 2')).toBeInTheDocument()
    })

    it('displays item prices', () => {
      vi.mocked(useCart).mockReturnValue({
        displayItems: mockCartItems,
        totals: { subtotal: 109.97, shipping: 0, tax: 0, total: 109.97 },
        isLoading: false,
        isAuthenticated: false,
        handleUpdateQuantity: vi.fn(),
        handleRemove: vi.fn(),
      })
      renderCart()
      expect(screen.getByText(/\$29\.99/)).toBeInTheDocument()
      expect(screen.getByText(/\$49\.99/)).toBeInTheDocument()
    })

    it('displays checkout button', () => {
      vi.mocked(useCart).mockReturnValue({
        displayItems: mockCartItems,
        totals: { subtotal: 109.97, shipping: 0, tax: 0, total: 109.97 },
        isLoading: false,
        isAuthenticated: false,
        handleUpdateQuantity: vi.fn(),
        handleRemove: vi.fn(),
      })
      renderCart()
      expect(screen.getByRole('button', { name: /checkout/i })).toBeInTheDocument()
    })
  })
})
