import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { render, screen, cleanup } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { Provider } from 'react-redux'
import { configureStore } from '@reduxjs/toolkit'
import Header from '../Header'
import { authSlice } from '../../store/slices/authSlice'
import type { AuthState } from '../../store/slices/authSlice'
import { cartSlice } from '../../store/slices/cartSlice'
import type { CartState } from '../../store/slices/cartSlice'

// Mock useNavigate
const mockNavigate = vi.fn()
vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual('react-router-dom')
  return {
    ...actual,
    useNavigate: () => mockNavigate,
  }
})

// Mock RTK Query hooks
vi.mock('../../store/api/cartApi', () => ({
  useGetCartQuery: vi.fn(() => ({ data: null, isLoading: false })),
}))

vi.mock('../../store/api/wishlistApi', () => ({
  useGetWishlistQuery: vi.fn(() => ({ data: null, isLoading: false })),
}))

const createTestStore = (authState: Partial<AuthState> = {}, cartState: Partial<CartState> = {}) => {
  return configureStore({
    reducer: {
      auth: authSlice.reducer,
      cart: cartSlice.reducer,
    },
    preloadedState: {
      auth: {
        isAuthenticated: false,
        user: null,
        loading: false,
        error: null,
        initialized: false,
        ...authState,
      } as AuthState,
      cart: {
        items: [],
        lastUpdated: Date.now(),
        ...cartState,
      } as CartState,
    },
  })
}

const renderHeader = (store = createTestStore()) => {
  return render(
    <Provider store={store}>
      <MemoryRouter>
        <Header />
      </MemoryRouter>
    </Provider>
  )
}

describe('Header', () => {
  beforeEach(() => {
    mockNavigate.mockClear()
  })

  afterEach(() => {
    cleanup()
  })

  describe('logo and branding', () => {
    it('should render the logo', () => {
      renderHeader()

      expect(screen.getByText('E-Shop')).toBeInTheDocument()
    })

    it('should have logo link to home', () => {
      renderHeader()

      const logoLink = screen.getByRole('link', { name: /e-shop/i })
      expect(logoLink).toHaveAttribute('href', '/')
    })
  })

  describe('navigation links', () => {
    it('should render Products link', () => {
      renderHeader()

      // Use getAllByText since there might be multiple (desktop + mobile)
      const productsLinks = screen.getAllByText('Products')
      expect(productsLinks.length).toBeGreaterThan(0)
    })

    it('should not render Orders link when not authenticated', () => {
      renderHeader()

      expect(screen.queryAllByText('Orders')).toHaveLength(0)
    })

    it('should render Orders link when authenticated', () => {
      const store = createTestStore({
        isAuthenticated: true,
        user: { id: '1', email: 'test@test.com', firstName: 'John', lastName: 'Doe', role: 'Customer' },
      })

      renderHeader(store)

      const ordersLinks = screen.getAllByText('Orders')
      expect(ordersLinks.length).toBeGreaterThan(0)
    })
  })

  describe('cart icon', () => {
    it('should render cart link', () => {
      renderHeader()

      // Use querySelector since the link has aria-label
      const cartLink = document.querySelector('a[href="/cart"]')
      expect(cartLink).toBeTruthy()
    })

    it('should not show cart badge when cart is empty', () => {
      renderHeader()

      expect(screen.queryByText('0')).not.toBeInTheDocument()
    })

    it('should show cart badge with item count', () => {
      const store = createTestStore({}, {
        items: [
          { id: '1', name: 'Product 1', slug: 'product-1', price: 10, quantity: 2, maxStock: 10, image: '' },
          { id: '2', name: 'Product 2', slug: 'product-2', price: 20, quantity: 3, maxStock: 10, image: '' },
        ],
      })

      renderHeader(store)

      expect(screen.getByText('5')).toBeInTheDocument()
    })

    it('should show 99+ when cart has more than 99 items', () => {
      const store = createTestStore({}, {
        items: [
          { id: '1', name: 'Product 1', slug: 'product-1', price: 10, quantity: 100, maxStock: 100, image: '' },
        ],
      })

      renderHeader(store)

      expect(screen.getByText('99+')).toBeInTheDocument()
    })
  })

  describe('wishlist icon', () => {
    it('should not show wishlist when not authenticated', () => {
      renderHeader()

      const wishlistLink = document.querySelector('a[href="/wishlist"]')
      expect(wishlistLink).toBeNull()
    })

    it('should show wishlist when authenticated', () => {
      const store = createTestStore({
        isAuthenticated: true,
        user: { id: '1', email: 'test@test.com', firstName: 'John', lastName: 'Doe', role: 'Customer' },
      })

      renderHeader(store)

      const wishlistLink = document.querySelector('a[href="/wishlist"]')
      expect(wishlistLink).toBeTruthy()
    })
  })

  describe('authentication buttons - not authenticated', () => {
    it('should render Sign In button when not authenticated', () => {
      renderHeader()

      expect(screen.getByText('Sign In')).toBeInTheDocument()
    })

    it('should render Sign Up button when not authenticated', () => {
      renderHeader()

      expect(screen.getByText('Sign Up')).toBeInTheDocument()
    })

    it('should have Sign In link to login page', () => {
      renderHeader()

      const signInButton = screen.getByText('Sign In')
      const link = signInButton.closest('a')
      expect(link).toHaveAttribute('href', '/login')
    })

    it('should have Sign Up link to register page', () => {
      renderHeader()

      const signUpButton = screen.getByText('Sign Up')
      const link = signUpButton.closest('a')
      expect(link).toHaveAttribute('href', '/register')
    })
  })

  describe('user menu - authenticated', () => {
    it('should show user avatar with first letter of name', () => {
      const store = createTestStore({
        isAuthenticated: true,
        user: { id: '1', email: 'test@test.com', firstName: 'John', lastName: 'Doe', role: 'Customer' },
      })

      renderHeader(store)

      expect(screen.getByText('J')).toBeInTheDocument()
    })

    it('should show user first name', () => {
      const store = createTestStore({
        isAuthenticated: true,
        user: { id: '1', email: 'test@test.com', firstName: 'John', lastName: 'Doe', role: 'Customer' },
      })

      renderHeader(store)

      expect(screen.getByText('John')).toBeInTheDocument()
    })

    it('should open user menu when clicked', async () => {
      const user = userEvent.setup()
      const store = createTestStore({
        isAuthenticated: true,
        user: { id: '1', email: 'test@test.com', firstName: 'John', lastName: 'Doe', role: 'Customer' },
      })

      renderHeader(store)

      // Find the button by its aria-label using querySelector
      const userButton = document.querySelector('[aria-label="User menu"]') as HTMLButtonElement
      expect(userButton).toBeTruthy()
      await user.click(userButton!)

      expect(screen.getByText('My Profile')).toBeInTheDocument()
      expect(screen.getByText('Logout')).toBeInTheDocument()
    })

    it('should show user email in dropdown', async () => {
      const user = userEvent.setup()
      const store = createTestStore({
        isAuthenticated: true,
        user: { id: '1', email: 'test@test.com', firstName: 'John', lastName: 'Doe', role: 'Customer' },
      })

      renderHeader(store)

      const userButton = document.querySelector('[aria-label="User menu"]') as HTMLButtonElement
      await user.click(userButton!)

      expect(screen.getByText('test@test.com')).toBeInTheDocument()
    })

    it('should close user menu when clicking outside', async () => {
      const user = userEvent.setup()
      const store = createTestStore({
        isAuthenticated: true,
        user: { id: '1', email: 'test@test.com', firstName: 'John', lastName: 'Doe', role: 'Customer' },
      })

      renderHeader(store)

      const userButton = document.querySelector('[aria-label="User menu"]') as HTMLButtonElement
      await user.click(userButton!)
      expect(screen.getByText('My Profile')).toBeInTheDocument()

      // Click outside
      await user.click(document.body)

      expect(screen.queryByText('My Profile')).not.toBeInTheDocument()
    })
  })

  describe('mobile menu', () => {
    it('should have mobile menu button', () => {
      renderHeader()

      expect(screen.getByRole('button', { name: 'Toggle menu' })).toBeInTheDocument()
    })

    it('should open mobile menu when clicked', async () => {
      const user = userEvent.setup()
      renderHeader()

      const menuButton = screen.getByRole('button', { name: 'Toggle menu' })
      await user.click(menuButton)

      // Mobile menu should show Cart link
      expect(screen.getByText('Cart')).toBeInTheDocument()
    })

    it('should show Sign In and Sign Up in mobile menu when not authenticated', async () => {
      const user = userEvent.setup()
      renderHeader()

      const menuButton = screen.getByRole('button', { name: 'Toggle menu' })
      await user.click(menuButton)

      // There should be Sign In links (desktop + mobile)
      const signInLinks = screen.getAllByText('Sign In')
      expect(signInLinks.length).toBeGreaterThan(0)
    })

    it('should close mobile menu when a link is clicked', async () => {
      const user = userEvent.setup()
      renderHeader()

      const menuButton = screen.getByRole('button', { name: 'Toggle menu' })
      await user.click(menuButton)

      // Click on Cart link in mobile menu
      const cartLinks = screen.getAllByText('Cart')
      await user.click(cartLinks[0])

      // Mobile menu should close - Cart text should not be visible
      expect(screen.queryByText('Cart')).not.toBeInTheDocument()
    })
  })

  describe('logout', () => {
    it('should dispatch logout and navigate on logout click', async () => {
      const user = userEvent.setup()
      const store = createTestStore({
        isAuthenticated: true,
        user: { id: '1', email: 'test@test.com', firstName: 'John', lastName: 'Doe', role: 'Customer' },
      })

      renderHeader(store)

      // Open user menu
      const userButton = document.querySelector('[aria-label="User menu"]') as HTMLButtonElement
      await user.click(userButton!)

      // Click logout - find by text since the button doesn't have an accessible name
      const logoutButton = screen.getByText('Logout').closest('button')
      expect(logoutButton).toBeTruthy()
      await user.click(logoutButton!)

      expect(mockNavigate).toHaveBeenCalledWith('/')
    })
  })
})
