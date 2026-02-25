import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { render, screen, cleanup } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { Input } from '../ui/Input'

describe('Input', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  afterEach(() => {
    cleanup()
  })

  describe('rendering', () => {
    it('should render an input element', () => {
      render(<Input />)

      expect(screen.getByRole('textbox')).toBeInTheDocument()
    })

    it('should render with a label', () => {
      render(<Input label="Email" />)

      expect(screen.getByLabelText('Email')).toBeInTheDocument()
    })

    it('should render without a label', () => {
      render(<Input placeholder="Enter email" />)

      expect(screen.getByPlaceholderText('Enter email')).toBeInTheDocument()
    })

    it('should forward ref correctly', () => {
      const ref = vi.fn()
      render(<Input ref={ref} />)

      expect(ref).toHaveBeenCalled()
    })
  })

  describe('variants', () => {
    it('should apply default variant by default', () => {
      render(<Input />)

      const input = screen.getByRole('textbox')
      expect(input.className).toContain('inputDefault')
    })

    it('should apply subtle variant', () => {
      render(<Input variant="subtle" />)

      const input = screen.getByRole('textbox')
      expect(input.className).toContain('inputSubtle')
    })

    it('should apply error variant when error prop is provided', () => {
      render(<Input error="This field is required" />)

      const input = screen.getByRole('textbox')
      expect(input.className).toContain('inputError')
    })

    it('should override variant with error variant when error is present', () => {
      render(<Input variant="subtle" error="Error message" />)

      const input = screen.getByRole('textbox')
      expect(input.className).toContain('inputError')
    })
  })

  describe('error state', () => {
    it('should display error message', () => {
      render(<Input error="This field is required" />)

      expect(screen.getByText('This field is required')).toBeInTheDocument()
    })

    it('should not display error message when error is not provided', () => {
      render(<Input />)

      expect(screen.queryByRole('alert')).not.toBeInTheDocument()
    })

    it('should have error styling when error is present', () => {
      render(<Input error="Error" />)

      const errorText = screen.getByText('Error')
      expect(errorText.className).toContain('error')
    })
  })

  describe('icon', () => {
    it('should render icon when provided', () => {
      render(<Input icon={<span data-testid="test-icon">🔍</span>} />)

      expect(screen.getByTestId('test-icon')).toBeInTheDocument()
    })

    it('should apply inputWithIcon class when icon is present', () => {
      render(<Input icon={<span>🔍</span>} />)

      const input = screen.getByRole('textbox')
      expect(input.className).toContain('inputWithIcon')
    })

    it('should not apply inputWithIcon class when icon is not present', () => {
      render(<Input />)

      const input = screen.getByRole('textbox')
      expect(input.className).not.toContain('inputWithIcon')
    })
  })

  describe('interactions', () => {
    it('should accept user input', async () => {
      const user = userEvent.setup()
      render(<Input />)

      const input = screen.getByRole('textbox')
      await user.type(input, 'test@example.com')

      expect(input).toHaveValue('test@example.com')
    })

    it('should call onChange handler', async () => {
      const user = userEvent.setup()
      const handleChange = vi.fn()
      render(<Input onChange={handleChange} />)

      const input = screen.getByRole('textbox')
      await user.type(input, 'test')

      expect(handleChange).toHaveBeenCalled()
    })

    it('should call onFocus handler', async () => {
      const user = userEvent.setup()
      const handleFocus = vi.fn()
      render(<Input onFocus={handleFocus} />)

      const input = screen.getByRole('textbox')
      await user.click(input)

      expect(handleFocus).toHaveBeenCalled()
    })

    it('should call onBlur handler', async () => {
      const user = userEvent.setup()
      const handleBlur = vi.fn()
      render(<Input onBlur={handleBlur} />)

      const input = screen.getByRole('textbox')
      await user.click(input)
      await user.tab()

      expect(handleBlur).toHaveBeenCalled()
    })
  })

  describe('disabled state', () => {
    it('should be disabled when disabled prop is true', () => {
      render(<Input disabled />)

      expect(screen.getByRole('textbox')).toBeDisabled()
    })

    it('should not accept input when disabled', async () => {
      const user = userEvent.setup()
      render(<Input disabled />)

      const input = screen.getByRole('textbox')
      await user.type(input, 'test')

      expect(input).toHaveValue('')
    })
  })

  describe('HTML input attributes', () => {
    it('should pass through type attribute', () => {
      render(<Input type="email" />)

      const input = screen.getByRole('textbox')
      expect(input).toHaveAttribute('type', 'email')
    })

    it('should pass through placeholder attribute', () => {
      render(<Input placeholder="Enter your email" />)

      expect(screen.getByPlaceholderText('Enter your email')).toBeInTheDocument()
    })

    it('should pass through name attribute', () => {
      render(<Input name="email" />)

      const input = screen.getByRole('textbox')
      expect(input).toHaveAttribute('name', 'email')
    })

    it('should pass through id attribute', () => {
      render(<Input id="email-input" />)

      const input = screen.getByRole('textbox')
      expect(input).toHaveAttribute('id', 'email-input')
    })

    it('should pass through required attribute', () => {
      render(<Input required />)

      const input = screen.getByRole('textbox')
      expect(input).toBeRequired()
    })

    it('should pass through maxLength attribute', () => {
      render(<Input maxLength={50} />)

      const input = screen.getByRole('textbox')
      expect(input).toHaveAttribute('maxLength', '50')
    })

    it('should pass through autoComplete attribute', () => {
      render(<Input autoComplete="email" />)

      const input = screen.getByRole('textbox')
      expect(input).toHaveAttribute('autoComplete', 'email')
    })
  })

  describe('custom className', () => {
    it('should merge custom className with default classes', () => {
      render(<Input className="custom-input" />)

      const input = screen.getByRole('textbox')
      expect(input.className).toContain('custom-input')
      expect(input.className).toContain('input')
    })
  })

  describe('accessibility', () => {
    it('should associate label with input', () => {
      render(<Input label="Email" id="email" />)

      const input = screen.getByLabelText('Email')
      expect(input).toBeInTheDocument()
    })

    it('should be focusable', () => {
      render(<Input />)

      const input = screen.getByRole('textbox')
      input.focus()
      expect(input).toHaveFocus()
    })

    it('should not be focusable when disabled', () => {
      render(<Input disabled />)

      const input = screen.getByRole('textbox')
      input.focus()
      expect(input).not.toHaveFocus()
    })
  })

  describe('value handling', () => {
    it('should display controlled value', () => {
      render(<Input value="test@example.com" onChange={() => {}} />)

      expect(screen.getByDisplayValue('test@example.com')).toBeInTheDocument()
    })

    it('should display default value', () => {
      render(<Input defaultValue="default@example.com" />)

      expect(screen.getByDisplayValue('default@example.com')).toBeInTheDocument()
    })
  })
})