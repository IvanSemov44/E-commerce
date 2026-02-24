import { describe, it, expect, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { Provider } from 'react-redux'
import { configureStore } from '@reduxjs/toolkit'
import ProtectedRoute from '../ProtectedRoute'
import { authSlice } from '../../store/slices/authSlice'
import type { AuthState } from '../../store/slices/authSlice'

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
        initialized: false,
        ...authState,
      } as AuthState,
    },
  })
}

const renderProtectedRoute = (store = createTestStore()) => {
  return render(
    <Provider store={store}>
      <MemoryRouter>
        <ProtectedRoute>
          <div>Protected Content</div>
        </ProtectedRoute>
      </MemoryRouter>
    </Provider>
  )
}

// Mock Navigate component
vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual('react-router-dom')
  return {
    ...actual,
    Navigate: ({ to }: { to: string }) => <div data-testid="navigate" data-to={to} />,
  }
})

describe('ProtectedRoute', () => {
  describe('loading state', () => {
    it('should show loading spinner when loading', () => {
      const store = createTestStore({ loading: true })

      renderProtectedRoute(store)

      // Should show loading spinner
      const spinner = document.querySelector('.animate-spin')
      expect(spinner).toBeInTheDocument()
    })

    it('should not show protected content when loading', () => {
      const store = createTestStore({ loading: true })

      renderProtectedRoute(store)

      expect(screen.queryByText('Protected Content')).not.toBeInTheDocument()
    })
  })

  describe('unauthenticated state', () => {
    it('should redirect to login when not authenticated', () => {
      const store = createTestStore({ isAuthenticated: false, loading: false })

      renderProtectedRoute(store)

      expect(screen.getByTestId('navigate')).toHaveAttribute('data-to', '/login')
    })

    it('should not show protected content when not authenticated', () => {
      const store = createTestStore({ isAuthenticated: false, loading: false })

      renderProtectedRoute(store)

      expect(screen.queryByText('Protected Content')).not.toBeInTheDocument()
    })
  })

  describe('authenticated state', () => {
    it('should show protected content when authenticated', () => {
      const store = createTestStore({
        isAuthenticated: true,
        loading: false,
        user: { id: '1', email: 'test@test.com', firstName: 'John', lastName: 'Doe', role: 'Customer' },
      })

      renderProtectedRoute(store)

      expect(screen.getByText('Protected Content')).toBeInTheDocument()
    })

    it('should not redirect when authenticated', () => {
      const store = createTestStore({
        isAuthenticated: true,
        loading: false,
        user: { id: '1', email: 'test@test.com', firstName: 'John', lastName: 'Doe', role: 'Customer' },
      })

      renderProtectedRoute(store)

      expect(screen.queryByTestId('navigate')).not.toBeInTheDocument()
    })

    it('should not show loading spinner when authenticated', () => {
      const store = createTestStore({
        isAuthenticated: true,
        loading: false,
        user: { id: '1', email: 'test@test.com', firstName: 'John', lastName: 'Doe', role: 'Customer' },
      })

      renderProtectedRoute(store)

      const spinner = document.querySelector('.animate-spin')
      expect(spinner).not.toBeInTheDocument()
    })
  })
})