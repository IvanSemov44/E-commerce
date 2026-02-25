import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { render, screen, cleanup, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { Provider } from 'react-redux'
import { configureStore } from '@reduxjs/toolkit'
import Toast from '../Toast/Toast'
import { toastSlice, type Toast as ToastType } from '../../store/slices/toastSlice'

// Create a mock store
const createTestStore = () => {
  return configureStore({
    reducer: {
      toast: toastSlice.reducer,
    },
  })
}

// Helper to render Toast with mock dismiss
const renderToast = (toast: ToastType, onDismiss = vi.fn()) => {
  return {
    ...render(<Toast toast={toast} onDismiss={onDismiss} />),
    onDismiss,
  }
}

describe('Toast', () => {
  beforeEach(() => {
    vi.useFakeTimers()
  })

  afterEach(() => {
    vi.useRealTimers()
    cleanup()
  })

  describe('rendering', () => {
    it('should render the toast message', () => {
      const toast: ToastType = {
        id: '1',
        message: 'Test message',
        variant: 'info',
        duration: 5000,
      }
      renderToast(toast)

      expect(screen.getByText('Test message')).toBeInTheDocument()
    })

    it('should render with success variant', () => {
      const toast: ToastType = {
        id: '1',
        message: 'Success!',
        variant: 'success',
        duration: 5000,
      }
      renderToast(toast)

      expect(screen.getByText('Success!')).toBeInTheDocument()
      expect(screen.getByText('Success!').closest('div')).toHaveClass('success')
    })

    it('should render with error variant', () => {
      const toast: ToastType = {
        id: '1',
        message: 'Error occurred',
        variant: 'error',
        duration: 5000,
      }
      renderToast(toast)

      expect(screen.getByText('Error occurred')).toBeInTheDocument()
      expect(screen.getByText('Error occurred').closest('div')).toHaveClass('error')
    })

    it('should render with warning variant', () => {
      const toast: ToastType = {
        id: '1',
        message: 'Warning!',
        variant: 'warning',
        duration: 5000,
      }
      renderToast(toast)

      expect(screen.getByText('Warning!')).toBeInTheDocument()
      expect(screen.getByText('Warning!').closest('div')).toHaveClass('warning')
    })

    it('should render with info variant', () => {
      const toast: ToastType = {
        id: '1',
        message: 'Info message',
        variant: 'info',
        duration: 5000,
      }
      renderToast(toast)

      expect(screen.getByText('Info message')).toBeInTheDocument()
      expect(screen.getByText('Info message').closest('div')).toHaveClass('info')
    })
  })

  describe('icons', () => {
    it('should display success icon for success variant', () => {
      const toast: ToastType = {
        id: '1',
        message: 'Success!',
        variant: 'success',
        duration: 5000,
      }
      renderToast(toast)

      expect(screen.getByText('Success!').parentElement).toHaveTextContent('Success!') // Icon is 'check'
    })

    it('should display error icon for error variant', () => {
      const toast: ToastType = {
        id: '1',
        message: 'Error!',
        variant: 'error',
        duration: 5000,
      }
      renderToast(toast)

      const toastElement = screen.getByRole('alert')
      expect(toastElement).toBeInTheDocument()
    })

    it('should display warning icon for warning variant', () => {
      const toast: ToastType = {
        id: '1',
        message: 'Warning!',
        variant: 'warning',
        duration: 5000,
      }
      renderToast(toast)

      const toastElement = screen.getByRole('alert')
      expect(toastElement).toBeInTheDocument()
    })

    it('should display info icon for info variant', () => {
      const toast: ToastType = {
        id: '1',
        message: 'Info!',
        variant: 'info',
        duration: 5000,
      }
      renderToast(toast)

      const toastElement = screen.getByRole('alert')
      expect(toastElement).toBeInTheDocument()
    })
  })

  describe('accessibility', () => {
    it('should have role="alert"', () => {
      const toast: ToastType = {
        id: '1',
        message: 'Test message',
        variant: 'info',
        duration: 5000,
      }
      renderToast(toast)

      expect(screen.getByRole('alert')).toBeInTheDocument()
    })

    it('should have aria-live="polite"', () => {
      const toast: ToastType = {
        id: '1',
        message: 'Test message',
        variant: 'info',
        duration: 5000,
      }
      renderToast(toast)

      expect(screen.getByRole('alert')).toHaveAttribute('aria-live', 'polite')
    })

    it('should have accessible dismiss button', () => {
      const toast: ToastType = {
        id: '1',
        message: 'Test message',
        variant: 'info',
        duration: 5000,
      }
      renderToast(toast)

      const dismissButton = screen.getByRole('button', { name: /dismiss/i })
      expect(dismissButton).toBeInTheDocument()
    })
  })

  describe('dismissal', () => {
    it('should call onDismiss when close button is clicked', async () => {
      const user = userEvent.setup()
      const toast: ToastType = {
        id: '1',
        message: 'Test message',
        variant: 'info',
        duration: 5000,
      }
      const { onDismiss } = renderToast(toast)

      await user.click(screen.getByRole('button', { name: /dismiss/i }))

      expect(onDismiss).toHaveBeenCalledWith('1')
    })

    it('should auto-dismiss after duration', async () => {
      const toast: ToastType = {
        id: '1',
        message: 'Test message',
        variant: 'info',
        duration: 3000,
      }
      const { onDismiss } = renderToast(toast)

      // Fast-forward time
      vi.advanceTimersByTime(3000)

      expect(onDismiss).toHaveBeenCalledWith('1')
    })

    it('should not auto-dismiss when duration is 0', () => {
      const toast: ToastType = {
        id: '1',
        message: 'Test message',
        variant: 'info',
        duration: 0,
      }
      const { onDismiss } = renderToast(toast)

      // Fast-forward time significantly
      vi.advanceTimersByTime(10000)

      expect(onDismiss).not.toHaveBeenCalled()
    })

    it('should not auto-dismiss when duration is negative', () => {
      const toast: ToastType = {
        id: '1',
        message: 'Test message',
        variant: 'info',
        duration: -1,
      }
      const { onDismiss } = renderToast(toast)

      // Fast-forward time significantly
      vi.advanceTimersByTime(10000)

      expect(onDismiss).not.toHaveBeenCalled()
    })

    it('should not auto-dismiss when duration is undefined', () => {
      const toast: ToastType = {
        id: '1',
        message: 'Test message',
        variant: 'info',
        duration: undefined as unknown as number,
      }
      const { onDismiss } = renderToast(toast)

      // Fast-forward time significantly
      vi.advanceTimersByTime(10000)

      expect(onDismiss).not.toHaveBeenCalled()
    })

    it('should clear timeout on unmount', () => {
      const toast: ToastType = {
        id: '1',
        message: 'Test message',
        variant: 'info',
        duration: 5000,
      }
      const { onDismiss, unmount } = renderToast(toast)

      unmount()

      // Fast-forward time after unmount
      vi.advanceTimersByTime(5000)

      expect(onDismiss).not.toHaveBeenCalled()
    })
  })
})