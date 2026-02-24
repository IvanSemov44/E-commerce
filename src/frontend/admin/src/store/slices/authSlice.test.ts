import { describe, it, expect } from 'vitest'
import {
  authReducer,
  loginStart,
  loginSuccess,
  loginFailure,
  logout,
  clearError,
  setUser,
  setInitialized,
} from './authSlice'
import type { AuthState, AdminUser } from './authSlice'

describe('authSlice', () => {
  const initialState: AuthState = {
    isAuthenticated: false,
    user: null,
    loading: false,
    error: null,
    initialized: false,
  }

  const mockUser: AdminUser = {
    id: '1',
    email: 'admin@test.com',
    firstName: 'Admin',
    lastName: 'User',
    role: 'admin',
  }

  it('should return the initial state', () => {
    expect(authReducer(undefined, { type: 'unknown' })).toEqual(initialState)
  })

  it('should handle loginStart', () => {
    const state = authReducer(initialState, loginStart())
    expect(state.loading).toBe(true)
    expect(state.error).toBe(null)
  })

  it('should handle loginSuccess', () => {
    const loadingState = { ...initialState, loading: true }
    const state = authReducer(loadingState, loginSuccess(mockUser))
    
    expect(state.loading).toBe(false)
    expect(state.isAuthenticated).toBe(true)
    expect(state.user).toEqual(mockUser)
    expect(state.error).toBe(null)
    expect(state.initialized).toBe(true)
  })

  it('should handle loginFailure', () => {
    const loadingState = { ...initialState, loading: true }
    const state = authReducer(loadingState, loginFailure('Invalid credentials'))
    
    expect(state.loading).toBe(false)
    expect(state.isAuthenticated).toBe(false)
    expect(state.user).toBe(null)
    expect(state.error).toBe('Invalid credentials')
    expect(state.initialized).toBe(true)
  })

  it('should handle logout', () => {
    const loggedInState: AuthState = {
      isAuthenticated: true,
      user: mockUser,
      loading: false,
      error: null,
      initialized: true,
    }

    const state = authReducer(loggedInState, logout())
    
    expect(state.isAuthenticated).toBe(false)
    expect(state.user).toBe(null)
    expect(state.loading).toBe(false)
    expect(state.error).toBe(null)
    expect(state.initialized).toBe(true)
  })

  it('should handle clearError', () => {
    const errorState: AuthState = {
      ...initialState,
      error: 'Some error message',
    }

    const state = authReducer(errorState, clearError())
    expect(state.error).toBe(null)
  })

  it('should handle setUser', () => {
    const state = authReducer(initialState, setUser(mockUser))
    
    expect(state.user).toEqual(mockUser)
    expect(state.isAuthenticated).toBe(true)
    expect(state.initialized).toBe(true)
  })

  it('should handle setInitialized', () => {
    const state = authReducer(initialState, setInitialized())
    expect(state.initialized).toBe(true)
  })

  it('should handle superadmin role', () => {
    const superAdminUser: AdminUser = {
      ...mockUser,
      role: 'superadmin',
    }

    const state = authReducer(initialState, loginSuccess(superAdminUser))
    
    expect(state.user?.role).toBe('superadmin')
    expect(state.isAuthenticated).toBe(true)
  })

  it('should handle user with avatar', () => {
    const userWithAvatar: AdminUser = {
      ...mockUser,
      avatarUrl: 'https://example.com/avatar.jpg',
    }

    const state = authReducer(initialState, loginSuccess(userWithAvatar))
    
    expect(state.user?.avatarUrl).toBe('https://example.com/avatar.jpg')
  })

  it('should preserve initialized state on logout', () => {
    const loggedInState: AuthState = {
      isAuthenticated: true,
      user: mockUser,
      loading: false,
      error: 'some error',
      initialized: true,
    }

    const state = authReducer(loggedInState, logout())
    
    expect(state.initialized).toBe(true)
    expect(state.error).toBe(null)
  })
})