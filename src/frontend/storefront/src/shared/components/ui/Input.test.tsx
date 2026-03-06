import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { Input } from './Input';
import { SearchIcon } from '@/shared/components/icons';

describe('Input', () => {
  it('renders input element', () => {
    render(<Input />);
    const input = screen.getByRole('textbox');
    expect(input).toBeInTheDocument();
  });

  it('renders with label', () => {
    const { container } = render(<Input label="Email" />);
    const label = screen.getByText('Email');
    const wrapper = label.closest('div');
    const input = container.querySelector('input');

    expect(label.tagName).toBe('LABEL');
    expect(wrapper).toContainElement(input);
  });

  it('renders without label when not provided', () => {
    const { container } = render(<Input />);
    const label = container.querySelector('label');
    expect(label).not.toBeInTheDocument();
  });

  it('applies default variant', () => {
    render(<Input />);
    const input = screen.getByRole('textbox');
    expect(input.className).toMatch(/inputDefault/);
  });

  it('applies subtle variant', () => {
    render(<Input variant="subtle" />);
    const input = screen.getByRole('textbox');
    expect(input.className).toMatch(/inputSubtle/);
  });

  it('applies error variant when error is provided', () => {
    render(<Input error="This field is required" />);
    const input = screen.getByRole('textbox');
    expect(input.className).toMatch(/inputError/);
  });

  it('displays error message', () => {
    render(<Input error="Invalid email" />);
    expect(screen.getByText('Invalid email')).toBeInTheDocument();
  });

  it('does not display error message when not provided', () => {
    const { container } = render(<Input />);
    const errorElement = container.querySelector('[class*="error"]');
    if (errorElement) {
      expect(errorElement).toHaveTextContent('');
    }
  });

  it('renders with icon', () => {
    render(<Input icon={<SearchIcon />} />);
    const input = screen.getByRole('textbox');
    expect(input.className).toMatch(/inputWithIcon/);
  });

  it('accepts placeholder attribute', () => {
    render(<Input placeholder="Enter your name" />);
    const input = screen.getByPlaceholderText('Enter your name');
    expect(input).toBeInTheDocument();
  });

  it('handles input change', async () => {
    const handleChange = vi.fn();
    const user = userEvent.setup();
    render(<Input onChange={handleChange} />);

    const input = screen.getByRole('textbox');
    await user.type(input, 'test');

    expect(handleChange).toHaveBeenCalled();
  });

  it('can be disabled', () => {
    render(<Input disabled />);
    const input = screen.getByRole('textbox');
    expect(input).toBeDisabled();
  });

  it('has correct type attribute', () => {
    render(<Input type="email" />);
    const input = screen.getByRole('textbox');
    expect(input).toHaveAttribute('type', 'email');
  });

  it('accepts aria-label for accessibility', () => {
    render(<Input aria-label="Search products" />);
    const input = screen.getByRole('textbox', { name: /search products/i });
    expect(input).toBeInTheDocument();
  });

  it('applies custom className', () => {
    render(<Input className="custom-class" />);
    const input = screen.getByRole('textbox');
    expect(input).toHaveClass('custom-class');
  });

  it('combines variant icon and error state', () => {
    render(<Input variant="subtle" icon={<SearchIcon />} error="Invalid" />);
    const input = screen.getByRole('textbox');
    expect(input.className).toMatch(/inputError/);
    expect(input.className).toMatch(/inputWithIcon/);
  });

  it('renders required attribute', () => {
    render(<Input required />);
    const input = screen.getByRole('textbox');
    expect(input).toBeRequired();
  });

  it('supports min/max attributes for number inputs', () => {
    render(<Input type="number" min="1" max="100" />);
    const input = screen.getByRole('spinbutton');
    expect(input).toHaveAttribute('min', '1');
    expect(input).toHaveAttribute('max', '100');
  });
});
