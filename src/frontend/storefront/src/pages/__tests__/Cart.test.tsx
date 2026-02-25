import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { BrowserRouter } from 'react-router-dom'
import { Provider } from 'react-redux'
import { configureStore } from '@reduxjs/toolkit'
import Cart from '../Cart'
import { cartApi } from '../../store/api/cartApi'
import { authSlice } from '../../store/slices/authSlice'

// Mock the cart API
vi.mock('../../store/api/cartApi', () => ({
  cartApi: {
    useGetCartQuery: vi.fn(),
    useUpdateCartItemMutation: vi.fn(),
    useRemoveCartItemMutation: vi.fn(),
    useClearCartMutation: vi.fn(),
  },
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

const mockCart = {
  items: [
    {
      productId: '1',
      productName: 'Test Product 1',
      productSlug: 'test-product-1',
      price: 29.99,
      quantity: 2,
      imageUrl: 'https://example.com/image1.jpg',
    },
    {
      productId: '2',
      productName: 'Test Product 2',
      productSlug: 'test-product-2',
      price: 49.99,
      quantity: 1,
      imageUrl: 'https://example.com/image2.jpg',
    },
  ],
  total: 109.97,
}

const renderCart = (initialState = {}) => {
  const store = configureStore({
    reducer: {
      [cartApi.reducerPath]: (state = {}) => state,
      auth: authSlice.reducer,
    },
    preloadedState: {
      auth: { user: null, isAuthenticated: false, ...initialState },
    },
  })

  return render(
    <Provider store={store}>
      <BrowserRouter>
        <Cart />
      </BrowserRouter>
    </Provider>
  )
}

describe('Cart Page', () => {
  let user: ReturnType<typeof userEvent.setup>

  beforeEach(() => {
    user = userEvent.setup()
    vi.clearAllMocks()
  })

  describe('Loading State', () => {
    it('shows loading skeleton while fetching cart', () => {
      vi.mocked(cartApi.useGetCartQuery).mockReturnValue({
        data: undefined,
        isLoading: true,
        error: null,
        refetch: vi.fn(),
      } as any)

      renderCart()

      expect(screen.getByTestId('cart-skeleton') || screen.getByText(/loading/i)).toBeTruthy()
    })
  })

  describe('Empty Cart', () => {
    it('shows empty cart message when cart is empty', () => {
      vi.mocked(cartApi.useGetCartQuery).mockReturnValue({
        data: { items: [], total: 0 },
        isLoading: false,
        error: null,
        refetch: vi.fn(),
      } as any)

      renderCart()

      expect(screen.getByText(/your cart is empty/i)).toBeTruthy()
    })

    it('shows continue shopping button when cart is empty', () => {
      vi.mocked(cartApi.useGetCartQuery).mockReturnValue({
        data: { items: [], total: 0 },
        isLoading: false,
        error: null,
        refetch: vi.fn(),
      } as any)

      renderCart()

      expect(screen.getByRole('button', { name: /continue shopping/i })).toBeTruthy()
    })
  })

  describe('Cart with Items', () => {
    beforeEach(() => {
      vi.mocked(cartApi.useGetCartQuery).mockReturnValue({
        data: mockCart,
        isLoading: false,
        error: null,
        refetch: vi.fn(),
      } as any)

      vi.mocked(cartApi.useUpdateCartItemMutation).mockReturnValue([
        vi.fn().mockResolvedValue({}),
        { isLoading: false },
      ] as any)

      vi.mocked(cartApi.useRemoveCartItemMutation).mockReturnValue([
        vi.fn().mockResolvedValue({}),
        { isLoading: false },
      ] as any)

      vi.mocked(cartApi.useClearCartMutation).mockReturnValue([
        vi.fn().mockResolvedValue({}),
        { isLoading: false },
      ] as any)
    })

    it('displays cart items', () => {
      renderCart()

      expect(screen.getByText('Test Product 1')).toBeTruthy()
      expect(screen.getByText('Test Product 2')).toBeTruthy()
    })

    it('displays item quantities', () => {
      renderCart()

      expect(screen.getByText('2')).toBeTruthy()
    })

    it('displays item prices', () => {
      renderCart()

      expect(screen.getByText('$29.99')).toBeTruthy()
      expect(screen.getByText('$49.99')).toBeTruthy()
    })

    it('displays cart total', () => {
      renderCart()

      expect(screen.getByText('$109.97')).toBeTruthy()
    })

    it('displays checkout button', () => {
      renderCart()

      expect(screen.getByRole('button', { name: /proceed to checkout/i })).toBeTruthy()
    })
  })

  describe('Cart Actions', () => {
    it('navigates to checkout when checkout button is clicked', async () => {
      vi.mocked(cartApi.useGetCartQuery).mockReturnValue({
        data: mockCart,
        isLoading: false,
        error: null,
        refetch: vi.fn(),
      } as any)

      renderCart()

      const checkoutButton = screen.getByRole('button', { name: /proceed to checkout/i })
      await user.click(checkoutButton)

      expect(mockNavigate).toHaveBeenCalledWith('/checkout')
    })
  })

  describe('Error State', () => {
    it('shows error message when cart fetch fails', () => {
      vi.mocked(cartApi.useGetCartQuery).mockReturnValue({
        data: undefined,
        isLoading: false,
        error: { message: 'Failed to fetch cart' },
        refetch: vi.fn(),
      } as any)

      renderCart()

      expect(screen.getByText(/error/i) || screen.getByText(/failed/i)).toBeTruthy()
    })
  })
})
