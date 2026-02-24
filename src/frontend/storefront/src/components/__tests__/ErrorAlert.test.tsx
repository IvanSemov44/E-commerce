import { describe, it, expect, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import ErrorAlert from '../ErrorAlert'

describe('ErrorAlert', () => {
  describe('rendering', () => {
    it('should render error message', () => {
      render(<ErrorAlert message="Something went wrong" />)

      expect(screen.getByText('Something went wrong')).toBeInTheDocument()
    })

    it('should render with different messages', () => {
      render(<ErrorAlert message="Network error occurred" />)

      expect(screen.getByText('Network error occurred')).toBeInTheDocument()
    })
  })

  describe('dismiss button', () => {
    it('should not render dismiss button when onDismiss is not provided', () => {
      render(<ErrorAlert message="Error" />)

      expect(screen.queryByRole('button')).not.toBeInTheDocument()
    })

    it('should render dismiss button when onDismiss is provided', () => {
      render(<ErrorAlert message="Error" onDismiss={() => {}} />)

      expect(screen.getByRole('button', { name: 'Dismiss error' })).toBeInTheDocument()
    })

    it('should call onDismiss when dismiss button is clicked', async () => {
      const user = userEvent.setup()
      const onDismiss = vi.fn()

      render(<ErrorAlert message="Error" onDismiss={onDismiss} />)

      await user.click(screen.getByRole('button', { name: 'Dismiss error' }))

      expect(onDismiss).toHaveBeenCalledOnce()
    })
  })

  describe('accessibility', () => {
    it('should have accessible dismiss button', () => {
      render(<ErrorAlert message="Error" onDismiss={() => {}} />)

      const button = screen.getByRole('button', { name: 'Dismiss error' })
      expect(button).toBeInTheDocument()
    })

    it('should render svg icon in dismiss button', () => {
      render(<ErrorAlert message="Error" onDismiss={() => {}} />)

      const button = screen.getByRole('button', { name: 'Dismiss error' })
      const svg = button.querySelector('svg')
      expect(svg).toBeInTheDocument()
    })
  })
})