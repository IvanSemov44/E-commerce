import { describe, it, expect, vi, beforeEach } from 'vitest'
import { renderHook } from '@testing-library/react'
import { Provider } from 'react-redux'
import { configureStore } from '@reduxjs/toolkit'
import { useProfileForm } from '../useProfileForm'
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

// Mock API
vi.mock('../../store/api/profileApi', () => ({
  useGetProfileQuery: vi.fn(() => ({
    data: {
      id: '1',
      email: 'test@example.com',
      firstName: 'John',
      lastName: 'Doe',
      phone: '1234567890',
      avatarUrl: '',
    },
    isLoading: false,
  })),
  useUpdateProfileMutation: vi.fn(() => [
    vi.fn().mockResolvedValue({ data: {} }),
    { isLoading: false },
  ]),
  useChangePasswordMutation: vi.fn(() => [
    vi.fn().mockResolvedValue({ data: {} }),
    { isLoading: false },
  ]),
}))

describe('useProfileForm', () => {
  let store: ReturnType<typeof configureStore>

  const wrapper = ({ children }: { children: ReactNode }) => (
    <Provider store={store}>{children}</Provider>
  )

  beforeEach(() => {
    store = configureStore({
      reducer: {
        auth: authReducer,
      },
      preloadedState: {
        auth: {
          user: {
            id: '1',
            email: 'test@example.com',
            firstName: 'John',
            lastName: 'Doe',
            phone: '1234567890',
            role: 'customer',
          },
          isAuthenticated: true,
          loading: false,
          error: null,
          initialized: true,
        },
      },
    })
    vi.clearAllMocks()
  })

  it('should initialize with formData', () => {
    const { result } = renderHook(() => useProfileForm(), { wrapper })
    
    expect(result.current.formData.firstName).toBeDefined()
    expect(result.current.isEditMode).toBe(false)
    expect(result.current.isLoading).toBe(false)
  })

  it('should have handleSubmit function', () => {
    const { result } = renderHook(() => useProfileForm(), { wrapper })
    
    expect(typeof result.current.handleSubmit).toBe('function')
  })

  it('should have handleCancel function', () => {
    const { result } = renderHook(() => useProfileForm(), { wrapper })
    
    expect(typeof result.current.handleCancel).toBe('function')
  })

  it('should set edit mode', () => {
    const { result } = renderHook(() => useProfileForm(), { wrapper })
    
    result.current.setIsEditMode(true)
    
    expect(result.current.isEditMode).toBe(true)
  })

  it('should set form data', () => {
    const { result } = renderHook(() => useProfileForm(), { wrapper })
    
    result.current.setFormData({
      firstName: 'Jane',
      lastName: 'Smith',
      email: 'jane@example.com',
      phone: '9876543210',
      avatarUrl: '',
    })
    
    expect(result.current.formData.firstName).toBe('Jane')
    expect(result.current.formData.lastName).toBe('Smith')
  })
})
