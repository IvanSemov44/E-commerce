import { describe, it, expect } from 'vitest'
import { render, screen } from '@testing-library/react'
import { Provider } from 'react-redux'
import { configureStore } from '@reduxjs/toolkit'
import { BrowserRouter } from 'react-router-dom'
import ProtectedRoute from '../components/ProtectedRoute'

// Create a mock store with auth state
const createMockStore = (isAuthenticated: boolean) => 
  configureStore({
    reducer: {
      auth: () => ({
        isAuthenticated,
        user: isAuthenticated ? { id: '1', email: 'admin@test.com', role: 'Admin' } : null,
        isLoading: false,
        error: null,
      }),
    },
  })

describe('ProtectedRoute', () => {
  it('renders children when user is authenticated', () => {
    const store = createMockStore(true)
    
    render(
      <Provider store={store}>
        <BrowserRouter>
          <ProtectedRoute>
            <div>Protected Content</div>
          </ProtectedRoute>
        </BrowserRouter>
      </Provider>
    )
    
    expect(screen.getByText('Protected Content')).toBeInTheDocument()
  })

  it('redirects to login when user is not authenticated', () => {
    const store = createMockStore(false)
    
    render(
      <Provider store={store}>
        <BrowserRouter>
          <ProtectedRoute>
            <div>Protected Content</div>
          </ProtectedRoute>
        </BrowserRouter>
      </Provider>
    )
    
    // Should not render protected content
    expect(screen.queryByText('Protected Content')).not.toBeInTheDocument()
  })
})
