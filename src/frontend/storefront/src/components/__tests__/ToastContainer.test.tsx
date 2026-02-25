import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { render, screen, cleanup } from '@testing-library/react'
import { Provider } from 'react-redux'
import { configureStore } from '@reduxjs/toolkit'
import ToastContainer from '../Toast/ToastContainer'
import { toastSlice, type ToastState } from '../../store/slices/toastSlice'

// Create a mock store with custom initial state
const createTestStore = (initialState: Partial<ToastState> = {}) => {
  return configureStore({
    reducer: {
      toast: toastSlice.reducer,
    },
    preloadedState: {
      toast: {
        toasts: [],
        ...initialState,
      } as ToastState,
    },
  })
}

// Helper to render ToastContainer with store
const renderToastContainer = (store = createTestStore()) => {
  return render(
    <Provider store={store}>
      <ToastContainer />
    </Provider>
  )
}

describe('ToastContainer', () => {
  beforeEach(() => {
    vi.useFakeTimers()
  })

  afterEach(() => {
    vi.useRealTimers()
    cleanup()
  })

  describe('rendering', () => {
    it('should render without toasts', () => {
      renderToastContainer()

      // Container should exist but be empty
      expect(screen.queryByRole('alert')).not.toBeInTheDocument()
    })

    it('should render a single toast', () => {
      const store = createTestStore({
        toasts: [
          { id: '1', message: 'Test toast', variant: 'info', duration: 5000 },
        ],
      })
      renderToastContainer(store)

      expect(screen.getByText('Test toast')).toBeInTheDocument()
    })

    it('should render multiple toasts', () => {
      const store = createTestStore({
        toasts: [
          { id: '1', message: 'First toast', variant: 'info', duration: 5000 },
          { id: '2', message: 'Second toast', variant: 'success', duration: 5000 },
          { id: '3', message: 'Third toast', variant: 'error', duration: 5000 },
        ],
      })
      renderToastContainer(store)

      expect(screen.getByText('First toast')).toBeInTheDocument()
      expect(screen.getByText('Second toast')).toBeInTheDocument()
      expect(screen.getByText('Third toast')).toBeInTheDocument()
    })
  })

  describe('toast variants', () => {
    it('should render success toast with correct styling', () => {
      const store = createTestStore({
        toasts: [
          { id: '1', message: 'Success!', variant: 'success', duration: 5000 },
        ],
      })
      renderToastContainer(store)

      const toast = screen.getByText('Success!').closest('div')
      expect(toast).toHaveClass('success')
    })

    it('should render error toast with correct styling', () => {
      const store = createTestStore({
        toasts: [
          { id: '1', message: 'Error!', variant: 'error', duration: 5000 },
        ],
      })
      renderToastContainer(store)

      const toast = screen.getByText('Error!').closest('div')
      expect(toast).toHaveClass('error')
    })

    it('should render warning toast with correct styling', () => {
      const store = createTestStore({
        toasts: [
          { id: '1', message: 'Warning!', variant: 'warning', duration: 5000 },
        ],
      })
      renderToastContainer(store)

      const toast = screen.getByText('Warning!').closest('div')
      expect(toast).toHaveClass('warning')
    })

    it('should render info toast with correct styling', () => {
      const store = createTestStore({
        toasts: [
          { id: '1', message: 'Info!', variant: 'info', duration: 5000 },
        ],
      })
      renderToastContainer(store)

      const toast = screen.getByText('Info!').closest('div')
      expect(toast).toHaveClass('info')
    })
  })

  describe('accessibility', () => {
    it('should have all toasts with role="alert"', () => {
      const store = createTestStore({
        toasts: [
          { id: '1', message: 'First toast', variant: 'info', duration: 5000 },
          { id: '2', message: 'Second toast', variant: 'success', duration: 5000 },
        ],
      })
      renderToastContainer(store)

      const alerts = screen.getAllByRole('alert')
      expect(alerts).toHaveLength(2)
    })

    it('should have all toasts with aria-live="polite"', () => {
      const store = createTestStore({
        toasts: [
          { id: '1', message: 'Test toast', variant: 'info', duration: 5000 },
        ],
      })
      renderToastContainer(store)

      const alert = screen.getByRole('alert')
      expect(alert).toHaveAttribute('aria-live', 'polite')
    })
  })

  describe('dismissal', () => {
    it('should dismiss toast when close button is clicked', async () => {
      const { userEvent } = await import('@testing-library/user-event')
      const user = userEvent.setup({ delay: null })
      
      const store = createTestStore({
        toasts: [
          { id: '1', message: 'Test toast', variant: 'info', duration: 5000 },
        ],
      })
      renderToastContainer(store)

      await user.click(screen.getByRole('button', { name: /dismiss/i }))

      // Toast should be removed from state
      expect(store.getState().toast.toasts).toHaveLength(0)
    })

    it('should dismiss only the clicked toast', async () => {
      const { userEvent } = await import('@testing-library/user-event')
      const user = userEvent.setup({ delay: null })
      
      const store = createTestStore({
        toasts: [
          { id: '1', message: 'First toast', variant: 'info', duration: 5000 },
          { id: '2', message: 'Second toast', variant: 'success', duration: 5000 },
        ],
      })
      renderToastContainer(store)

      const dismissButtons = screen.getAllByRole('button', { name: /dismiss/i })
      await user.click(dismissButtons[0])

      // Only first toast should be removed
      expect(store.getState().toast.toasts).toHaveLength(1)
      expect(store.getState().toast.toasts[0].id).toBe('2')
    })
  })
})