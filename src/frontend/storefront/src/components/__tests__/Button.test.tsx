import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { render, screen, cleanup } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { Button } from '../ui/Button'

describe('Button', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  afterEach(() => {
    cleanup()
  })

  describe('rendering', () => {
    it('should render children correctly', () => {
      render(<Button>Click me</Button>)

      expect(screen.getByRole('button', { name: 'Click me' })).toBeInTheDocument()
    })

    it('should render as a button element', () => {
      render(<Button>Button</Button>)

      expect(screen.getByRole('button')).toBeInTheDocument()
    })

    it('should forward ref correctly', () => {
      const ref = vi.fn()
      render(<Button ref={ref}>Button</Button>)

      expect(ref).toHaveBeenCalled()
    })
  })

  describe('variants', () => {
    it('should apply primary variant by default', () => {
      render(<Button>Primary</Button>)

      const button = screen.getByRole('button')
      expect(button.className).toContain('buttonPrimary')
    })

    it('should apply secondary variant', () => {
      render(<Button variant="secondary">Secondary</Button>)

      const button = screen.getByRole('button')
      expect(button.className).toContain('buttonSecondary')
    })

    it('should apply ghost variant', () => {
      render(<Button variant="ghost">Ghost</Button>)

      const button = screen.getByRole('button')
      expect(button.className).toContain('buttonGhost')
    })

    it('should apply destructive variant', () => {
      render(<Button variant="destructive">Destructive</Button>)

      const button = screen.getByRole('button')
      expect(button.className).toContain('buttonDestructive')
    })

    it('should apply outline variant', () => {
      render(<Button variant="outline">Outline</Button>)

      const button = screen.getByRole('button')
      expect(button.className).toContain('buttonOutline')
    })
  })

  describe('sizes', () => {
    it('should apply medium size by default', () => {
      render(<Button>Medium</Button>)

      const button = screen.getByRole('button')
      expect(button.className).toContain('buttonMedium')
    })

    it('should apply small size', () => {
      render(<Button size="sm">Small</Button>)

      const button = screen.getByRole('button')
      expect(button.className).toContain('buttonSmall')
    })

    it('should apply large size', () => {
      render(<Button size="lg">Large</Button>)

      const button = screen.getByRole('button')
      expect(button.className).toContain('buttonLarge')
    })
  })

  describe('disabled state', () => {
    it('should be disabled when disabled prop is true', () => {
      render(<Button disabled>Disabled</Button>)

      expect(screen.getByRole('button')).toBeDisabled()
    })

    it('should be disabled when isLoading is true', () => {
      render(<Button isLoading>Loading</Button>)

      expect(screen.getByRole('button')).toBeDisabled()
    })

    it('should have disabled styles when disabled', () => {
      render(<Button disabled>Disabled</Button>)

      const button = screen.getByRole('button')
      expect(button).toHaveStyle({ opacity: '0.5', cursor: 'not-allowed' })
    })

    it('should have disabled styles when loading', () => {
      render(<Button isLoading>Loading</Button>)

      const button = screen.getByRole('button')
      expect(button).toHaveStyle({ opacity: '0.5', cursor: 'not-allowed' })
    })
  })

  describe('loading state', () => {
    it('should show spinner when loading', () => {
      render(<Button isLoading>Loading</Button>)

      const spinner = document.querySelector('svg')
      expect(spinner).toBeInTheDocument()
    })

    it('should not show spinner when not loading', () => {
      render(<Button>Not Loading</Button>)

      const spinner = document.querySelector('svg')
      expect(spinner).not.toBeInTheDocument()
    })

    it('should still show children when loading', () => {
      render(<Button isLoading>Loading</Button>)

      expect(screen.getByText('Loading')).toBeInTheDocument()
    })
  })

  describe('interactions', () => {
    it('should call onClick when clicked', async () => {
      const user = userEvent.setup()
      const handleClick = vi.fn()
      render(<Button onClick={handleClick}>Click me</Button>)

      await user.click(screen.getByRole('button'))

      expect(handleClick).toHaveBeenCalledTimes(1)
    })

    it('should not call onClick when disabled', async () => {
      const user = userEvent.setup()
      const handleClick = vi.fn()
      render(<Button disabled onClick={handleClick}>Disabled</Button>)

      await user.click(screen.getByRole('button'))

      expect(handleClick).not.toHaveBeenCalled()
    })

    it('should not call onClick when loading', async () => {
      const user = userEvent.setup()
      const handleClick = vi.fn()
      render(<Button isLoading onClick={handleClick}>Loading</Button>)

      await user.click(screen.getByRole('button'))

      expect(handleClick).not.toHaveBeenCalled()
    })
  })

  describe('custom className', () => {
    it('should merge custom className with default classes', () => {
      render(<Button className="custom-class">Custom</Button>)

      const button = screen.getByRole('button')
      expect(button.className).toContain('custom-class')
      expect(button.className).toContain('button')
    })
  })

  describe('HTML button attributes', () => {
    it('should pass through type attribute', () => {
      render(<Button type="submit">Submit</Button>)

      expect(screen.getByRole('button')).toHaveAttribute('type', 'submit')
    })

    it('should pass through form attribute', () => {
      render(<Button form="my-form">Submit</Button>)

      expect(screen.getByRole('button')).toHaveAttribute('form', 'my-form')
    })

    it('should pass through name attribute', () => {
      render(<Button name="submit-btn">Submit</Button>)

      expect(screen.getByRole('button')).toHaveAttribute('name', 'submit-btn')
    })

    it('should pass through aria-label attribute', () => {
      render(<Button aria-label="Submit form">Submit</Button>)

      expect(screen.getByRole('button')).toHaveAttribute('aria-label', 'Submit form')
    })

    it('should pass through data attributes', () => {
      render(<Button data-testid="custom-button">Button</Button>)

      expect(screen.getByTestId('custom-button')).toBeInTheDocument()
    })
  })

  describe('accessibility', () => {
    it('should have correct role', () => {
      render(<Button>Button</Button>)

      expect(screen.getByRole('button')).toBeInTheDocument()
    })

    it('should be focusable', () => {
      render(<Button>Focusable</Button>)

      const button = screen.getByRole('button')
      button.focus()
      expect(button).toHaveFocus()
    })

    it('should not be focusable when disabled', () => {
      render(<Button disabled>Disabled</Button>)

      const button = screen.getByRole('button')
      button.focus()
      expect(button).not.toHaveFocus()
    })
  })
})