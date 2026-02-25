import { describe, it, expect, beforeEach } from 'vitest'
import { authSlice, loginStart, loginSuccess, loginFailure, logout, setUser, updateUser, clearError, setInitialized, type AuthState, type AuthUser } from '../authSlice'

describe('authSlice', () => {
  let initialState: AuthState

  beforeEach(() => {
    initialState = {
      isAuthenticated: false,
      user: null,
      loading: false,
      error: null,
      initialized: false,
    }
  })

  describe('initial state', () => {
    it('should return the initial state', () => {
      const state = authSlice.reducer(undefined, { type: 'unknown' })
      expect(state).toEqual(initialState)
    })
  })

  describe('loginStart', () => {
    it('should set loading to true', () => {
      const state = authSlice.reducer(initialState, loginStart())
      expect(state.loading).toBe(true)
    })

    it('should clear any existing error', () => {
      const stateWithError = { ...initialState, error: 'Previous error' }
      const state = authSlice.reducer(stateWithError, loginStart())
      expect(state.error).toBeNull()
    })
  })

  describe('loginSuccess', () => {
    const user: AuthUser = { id: '1', email: 'test@example.com', firstName: 'John', lastName: 'Doe', role: 'Customer' }

    it('should set isAuthenticated to true', () => {
      const state = authSlice.reducer(initialState, loginSuccess(user))
      expect(state.isAuthenticated).toBe(true)
    })

    it('should set the user', () => {
      const state = authSlice.reducer(initialState, loginSuccess(user))
      expect(state.user).toEqual(user)
    })

    it('should set loading to false', () => {
      const loadingState = { ...initialState, loading: true }
      const state = authSlice.reducer(loadingState, loginSuccess(user))
      expect(state.loading).toBe(false)
    })

    it('should set initialized to true', () => {
      const state = authSlice.reducer(initialState, loginSuccess(user))
      expect(state.initialized).toBe(true)
    })

    it('should clear any error', () => {
      const stateWithError = { ...initialState, error: 'Previous error' }
      const state = authSlice.reducer(stateWithError, loginSuccess(user))
      expect(state.error).toBeNull()
    })
  })

  describe('loginFailure', () => {
    it('should set the error', () => {
      const state = authSlice.reducer(initialState, loginFailure('Invalid credentials'))
      expect(state.error).toBe('Invalid credentials')
    })

    it('should set loading to false', () => {
      const loadingState = { ...initialState, loading: true }
      const state = authSlice.reducer(loadingState, loginFailure('Error'))
      expect(state.loading).toBe(false)
    })

    it('should set initialized to true', () => {
      const state = authSlice.reducer(initialState, loginFailure('Error'))
      expect(state.initialized).toBe(true)
    })
  })

  describe('logout', () => {
    it('should set isAuthenticated to false', () => {
      const authenticatedState = { ...initialState, isAuthenticated: true }
      const state = authSlice.reducer(authenticatedState, logout())
      expect(state.isAuthenticated).toBe(false)
    })

    it('should clear the user', () => {
      const userState = { ...initialState, user: { id: '1', email: 'test@example.com', firstName: 'John', lastName: 'Doe', role: 'Customer' } }
      const state = authSlice.reducer(userState, logout())
      expect(state.user).toBeNull()
    })

    it('should clear the error', () => {
      const stateWithError = { ...initialState, error: 'Some error' }
      const state = authSlice.reducer(stateWithError, logout())
      expect(state.error).toBeNull()
    })

    it('should set loading to false', () => {
      const loadingState = { ...initialState, loading: true }
      const state = authSlice.reducer(loadingState, logout())
      expect(state.loading).toBe(false)
    })

    it('should set initialized to true', () => {
      const state = authSlice.reducer(initialState, logout())
      expect(state.initialized).toBe(true)
    })
  })

  describe('setUser', () => {
    const user: AuthUser = { id: '1', email: 'test@example.com', firstName: 'John', lastName: 'Doe', role: 'Customer' }

    it('should set the user', () => {
      const state = authSlice.reducer(initialState, setUser(user))
      expect(state.user).toEqual(user)
    })

    it('should set isAuthenticated to true', () => {
      const state = authSlice.reducer(initialState, setUser(user))
      expect(state.isAuthenticated).toBe(true)
    })

    it('should set initialized to true', () => {
      const state = authSlice.reducer(initialState, setUser(user))
      expect(state.initialized).toBe(true)
    })
  })

  describe('updateUser', () => {
    const existingUser: AuthUser = { id: '1', email: 'test@example.com', firstName: 'John', lastName: 'Doe', role: 'Customer' }
    const userState = { ...initialState, user: existingUser }

    it('should update user data', () => {
      const updates = { firstName: 'Jane' }
      const state = authSlice.reducer(userState, updateUser(updates))
      expect(state.user?.firstName).toBe('Jane')
    })

    it('should not modify other user fields', () => {
      const updates = { firstName: 'Jane' }
      const state = authSlice.reducer(userState, updateUser(updates))
      expect(state.user?.lastName).toBe('Doe')
      expect(state.user?.email).toBe('test@example.com')
    })

    it('should not crash when user is null', () => {
      const state = authSlice.reducer(initialState, updateUser({ firstName: 'Jane' }))
      expect(state.user).toBeNull()
    })
  })

  describe('clearError', () => {
    it('should clear the error', () => {
      const stateWithError = { ...initialState, error: 'Some error' }
      const state = authSlice.reducer(stateWithError, clearError())
      expect(state.error).toBeNull()
    })

    it('should not modify other state fields', () => {
      const stateWithError = { 
        ...initialState, 
        error: 'Some error',
        isAuthenticated: true,
        loading: true,
        initialized: true
      }
      const state = authSlice.reducer(stateWithError, clearError())
      expect(state.isAuthenticated).toBe(true)
      expect(state.loading).toBe(true)
      expect(state.initialized).toBe(true)
    })
  })

  describe('setInitialized', () => {
    it('should set initialized to true', () => {
      const state = authSlice.reducer(initialState, setInitialized())
      expect(state.initialized).toBe(true)
    })

    it('should not modify other state fields', () => {
      const customState = { 
        ...initialState, 
        isAuthenticated: true,
        loading: true,
        error: 'Some error'
      }
      const state = authSlice.reducer(customState, setInitialized())
      expect(state.isAuthenticated).toBe(true)
      expect(state.loading).toBe(true)
      expect(state.error).toBe('Some error')
    })
  })
})