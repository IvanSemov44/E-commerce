import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { render, screen, cleanup } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { Provider } from 'react-redux'
import { configureStore } from '@reduxjs/toolkit'
import Header from '../Header'
import { authSlice, type AuthState } from '../../store/slices/authSlice'

// Mock useNavigate
const mockNavigate = vi.fn()
vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual('react-router-dom')
  return {
    ...actual,
    useNavigate: () => mockNavigate,
  }
})

const createTestStore = (authState: Partial<AuthState> = {}) => {
  return configureStore({
    reducer: {
      auth: authSlice.reducer,
    },
    preloadedState: {
      auth: {
        isAuthenticated: false,
        user: null,
        loading: false,
        error: null,
        ...authState,
      } as AuthState,
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

describe('Admin Header', () => {
  beforeEach(() => {
    mockNavigate.mockClear()
  })

  afterEach(() => {
    cleanup()
  })

  describe('rendering', () => {
    it('should render the dashboard title', () => {
      renderHeader()
      expect(screen.getByText('Admin Dashboard')).toBeInTheDocument()
    })

    it('should render notifications button', () => {
      renderHeader()
      expect(screen.getByRole('button', { name: /notifications/i })).toBeInTheDocument()
    })

    it('should render user menu button', () => {
      renderHeader()
      expect(screen.getByRole('button', { name: /user menu/i })).toBeInTheDocument()
    })

    it('should display user initials in avatar', () => {
      const store = createTestStore({
        isAuthenticated: true,
        user: { id: '1', email: 'admin@test.com', firstName: 'John', lastName: 'Doe', role: 'admin' },
      })
      renderHeader(store)

      expect(screen.getByText('J')).toBeInTheDocument()
    })

    it('should display default avatar when no user', () => {
      renderHeader()
      expect(screen.getByText('A')).toBeInTheDocument()
    })

    it('should display user first name', () => {
      const store = createTestStore({
        isAuthenticated: true,
        user: { id: '1', email: 'admin@test.com', firstName: 'John', lastName: 'Doe', role: 'admin' },
      })
      renderHeader(store)

      expect(screen.getByText('John')).toBeInTheDocument()
    })

    it('should display default name when no user', () => {
      renderHeader()
      expect(screen.getByText('Admin')).toBeInTheDocument()
    })
  })

  describe('user menu', () => {
    it('should open user menu when clicked', async () => {
      const user = userEvent.setup()
      const store = createTestStore({
        isAuthenticated: true,
        user: { id: '1', email: 'admin@test.com', firstName: 'John', lastName: 'Doe', role: 'admin' },
      })
      renderHeader(store)

      const menuButton = screen.getByRole('button', { name: /user menu/i })
      await user.click(menuButton)

      expect(screen.getByText('Logged in as')).toBeInTheDocument()
    })

    it('should display user info in dropdown', async () => {
      const user = userEvent.setup()
      const store = createTestStore({
        isAuthenticated: true,
        user: { id: '1', email: 'admin@test.com', firstName: 'John', lastName: 'Doe', role: 'admin' },
      })
      renderHeader(store)

      const menuButton = screen.getByRole('button', { name: /user menu/i })
      await user.click(menuButton)

      expect(screen.getByText('John Doe')).toBeInTheDocument()
      expect(screen.getByText('admin@test.com')).toBeInTheDocument()
    })

    it('should display Profile button', async () => {
      const user = userEvent.setup()
      const store = createTestStore({
        isAuthenticated: true,
        user: { id: '1', email: 'admin@test.com', firstName: 'John', lastName: 'Doe', role: 'admin' },
      })
      renderHeader(store)

      const menuButton = screen.getByRole('button', { name: /user menu/i })
      await user.click(menuButton)

      expect(screen.getByText('Profile')).toBeInTheDocument()
    })

    it('should display Logout button', async () => {
      const user = userEvent.setup()
      const store = createTestStore({
        isAuthenticated: true,
        user: { id: '1', email: 'admin@test.com', firstName: 'John', lastName: 'Doe', role: 'admin' },
      })
      renderHeader(store)

      const menuButton = screen.getByRole('button', { name: /user menu/i })
      await user.click(menuButton)

      expect(screen.getByText('Logout')).toBeInTheDocument()
    })

    it('should close menu when clicking outside', async () => {
      const user = userEvent.setup()
      const store = createTestStore({
        isAuthenticated: true,
        user: { id: '1', email: 'admin@test.com', firstName: 'John', lastName: 'Doe', role: 'admin' },
      })
      renderHeader(store)

      const menuButton = screen.getByRole('button', { name: /user menu/i })
      await user.click(menuButton)

      expect(screen.getByText('Logged in as')).toBeInTheDocument()

      // Click outside the menu
      await user.click(document.body)

      // Menu should be closed
      expect(screen.queryByText('Logged in as')).not.toBeInTheDocument()
    })
  })

  describe('logout', () => {
    it('should dispatch logout action when logout is clicked', async () => {
      const user = userEvent.setup()
      const store = createTestStore({
        isAuthenticated: true,
        user: { id: '1', email: 'admin@test.com', firstName: 'John', lastName: 'Doe', role: 'admin' },
      })
      renderHeader(store)

      const menuButton = screen.getByRole('button', { name: /user menu/i })
      await user.click(menuButton)

      const logoutButton = screen.getByText('Logout')
      await user.click(logoutButton)

      expect(store.getState().auth.isAuthenticated).toBe(false)
    })

    it('should navigate to login page after logout', async () => {
      const user = userEvent.setup()
      const store = createTestStore({
        isAuthenticated: true,
        user: { id: '1', email: 'admin@test.com', firstName: 'John', lastName: 'Doe', role: 'admin' },
      })
      renderHeader(store)

      const menuButton = screen.getByRole('button', { name: /user menu/i })
      await user.click(menuButton)

      const logoutButton = screen.getByText('Logout')
      await user.click(logoutButton)

      expect(mockNavigate).toHaveBeenCalledWith('/login')
    })
  })

  describe('accessibility', () => {
    it('should have aria-expanded attribute on user menu button', () => {
      renderHeader()

      const menuButton = screen.getByRole('button', { name: /user menu/i })
      expect(menuButton).toHaveAttribute('aria-expanded', 'false')
    })

    it('should update aria-expanded when menu is open', async () => {
      const user = userEvent.setup()
      const store = createTestStore({
        isAuthenticated: true,
        user: { id: '1', email: 'admin@test.com', firstName: 'John', lastName: 'Doe', role: 'admin' },
      })
      renderHeader(store)

      const menuButton = screen.getByRole('button', { name: /user menu/i })
      await user.click(menuButton)

      expect(menuButton).toHaveAttribute('aria-expanded', 'true')
    })
  })
})