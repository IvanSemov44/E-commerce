import { describe, it, expect, vi } from 'vitest'
import { render, screen, fireEvent } from '@testing-library/react'
import { Input } from './Input'

describe('Input', () => {
  it('renders correctly with placeholder', () => {
    render(<Input placeholder="Enter text" />)
    expect(screen.getByPlaceholderText('Enter text')).toBeInTheDocument()
  })

  it('handles value changes', () => {
    const handleChange = vi.fn()
    render(<Input onChange={handleChange} placeholder="Enter text" />)
    
    const input = screen.getByPlaceholderText('Enter text')
    fireEvent.change(input, { target: { value: 'test value' } })
    
    expect(handleChange).toHaveBeenCalled()
  })

  it('displays error message when error prop is provided', () => {
    render(<Input error="This field is required" placeholder="Enter text" />)
    expect(screen.getByText('This field is required')).toBeInTheDocument()
  })

  it('displays helper text when provided and no error', () => {
    render(<Input helperText="Enter your email address" placeholder="Enter text" />)
    expect(screen.getByText('Enter your email address')).toBeInTheDocument()
  })

  it('does not display helper text when error is present', () => {
    render(<Input helperText="Helper text" error="Error message" placeholder="Enter text" />)
    expect(screen.queryByText('Helper text')).not.toBeInTheDocument()
    expect(screen.getByText('Error message')).toBeInTheDocument()
  })

  it('displays label when provided', () => {
    render(<Input label="Username" placeholder="Enter username" />)
    expect(screen.getByLabelText('Username')).toBeInTheDocument()
  })

  it('is disabled when disabled prop is true', () => {
    render(<Input disabled placeholder="Enter text" />)
    expect(screen.getByPlaceholderText('Enter text')).toBeDisabled()
  })

  it('accepts different input types', () => {
    const { rerender } = render(<Input type="text" placeholder="Text input" />)
    expect(screen.getByPlaceholderText('Text input')).toHaveAttribute('type', 'text')
    
    rerender(<Input type="email" placeholder="Email input" />)
    expect(screen.getByPlaceholderText('Email input')).toHaveAttribute('type', 'email')
    
    rerender(<Input type="password" placeholder="Password input" />)
    expect(screen.getByPlaceholderText('Password input')).toHaveAttribute('type', 'password')
    
    rerender(<Input type="number" placeholder="Number input" />)
    expect(screen.getByPlaceholderText('Number input')).toHaveAttribute('type', 'number')
  })

  it('accepts additional className', () => {
    render(<Input className="custom-input" placeholder="Enter text" />)
    const input = screen.getByPlaceholderText('Enter text')
    expect(input.className).toContain('custom-input')
  })

  it('forwards ref correctly', () => {
    const ref = { current: null as HTMLInputElement | null }
    render(<Input ref={ref} placeholder="Enter text" />)
    expect(ref.current).toBeInstanceOf(HTMLInputElement)
  })

  it('generates unique id when id is not provided', () => {
    render(<Input label="Email" placeholder="Enter email" />)
    const input = screen.getByPlaceholderText('Enter email')
    expect(input.id).toMatch(/^input-/)
  })

  it('uses provided id when specified', () => {
    render(<Input id="custom-id" label="Email" placeholder="Enter email" />)
    const input = screen.getByPlaceholderText('Enter email')
    expect(input.id).toBe('custom-id')
  })

  it('applies error styling when error is present', () => {
    render(<Input error="Error" placeholder="Enter text" />)
    const input = screen.getByPlaceholderText('Enter text')
    expect(input.className).toMatch(/error/i)
  })
})