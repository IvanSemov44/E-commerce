import { describe, it, expect } from 'vitest'
import toastReducer, {
  addToast,
  removeToast,
  clearToasts,
} from './toastSlice'
import type { ToastState, Toast } from './toastSlice'

describe('toastSlice', () => {
  const initialState: ToastState = {
    toasts: [],
  }

  it('should return the initial state', () => {
    expect(toastReducer(undefined, { type: 'unknown' })).toEqual(initialState)
  })

  it('should handle addToast', () => {
    const toast: Toast = {
      id: '1',
      message: 'Test message',
      variant: 'success',
    }

    const state = toastReducer(initialState, addToast(toast))
    
    expect(state.toasts).toHaveLength(1)
    expect(state.toasts[0]).toEqual(toast)
  })

  it('should handle addToast with duration', () => {
    const toast: Toast = {
      id: '2',
      message: 'Test with duration',
      variant: 'info',
      duration: 5000,
    }

    const state = toastReducer(initialState, addToast(toast))
    
    expect(state.toasts[0].duration).toBe(5000)
  })

  it('should handle multiple toasts', () => {
    const toast1: Toast = { id: '1', message: 'First', variant: 'success' }
    const toast2: Toast = { id: '2', message: 'Second', variant: 'error' }

    let state = toastReducer(initialState, addToast(toast1))
    state = toastReducer(state, addToast(toast2))

    expect(state.toasts).toHaveLength(2)
    expect(state.toasts[0].message).toBe('First')
    expect(state.toasts[1].message).toBe('Second')
  })

  it('should handle all toast variants', () => {
    const variants: Array<Toast['variant']> = ['success', 'error', 'warning', 'info']

    variants.forEach((variant, index) => {
      const state = toastReducer(initialState, addToast({
        id: `toast-${index}`,
        message: `${variant} message`,
        variant,
      }))
      
      expect(state.toasts[0].variant).toBe(variant)
    })
  })

  it('should handle removeToast', () => {
    const toast1: Toast = { id: '1', message: 'First', variant: 'success' }
    const toast2: Toast = { id: '2', message: 'Second', variant: 'error' }

    let state = toastReducer(initialState, addToast(toast1))
    state = toastReducer(state, addToast(toast2))
    
    expect(state.toasts).toHaveLength(2)

    state = toastReducer(state, removeToast('1'))
    
    expect(state.toasts).toHaveLength(1)
    expect(state.toasts[0].id).toBe('2')
  })

  it('should not modify state when removing non-existent toast', () => {
    const toast: Toast = { id: '1', message: 'Test', variant: 'success' }
    
    let state = toastReducer(initialState, addToast(toast))
    state = toastReducer(state, removeToast('non-existent'))
    
    expect(state.toasts).toHaveLength(1)
  })

  it('should handle clearToasts', () => {
    const toast1: Toast = { id: '1', message: 'First', variant: 'success' }
    const toast2: Toast = { id: '2', message: 'Second', variant: 'error' }
    const toast3: Toast = { id: '3', message: 'Third', variant: 'warning' }

    let state = toastReducer(initialState, addToast(toast1))
    state = toastReducer(state, addToast(toast2))
    state = toastReducer(state, addToast(toast3))
    
    expect(state.toasts).toHaveLength(3)

    state = toastReducer(state, clearToasts())
    
    expect(state.toasts).toHaveLength(0)
  })

  it('should handle clearToasts on empty state', () => {
    const state = toastReducer(initialState, clearToasts())
    expect(state.toasts).toHaveLength(0)
  })

  it('should maintain toast order', () => {
    const toasts: Toast[] = [
      { id: '1', message: 'First', variant: 'success' },
      { id: '2', message: 'Second', variant: 'error' },
      { id: '3', message: 'Third', variant: 'warning' },
    ]

    let state = initialState
    toasts.forEach(toast => {
      state = toastReducer(state, addToast(toast))
    })

    state.toasts.forEach((toast, index) => {
      expect(toast.message).toBe(toasts[index].message)
    })
  })
})