import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { Input } from './Input';
import { EyeIcon } from '@/shared/components/icons';

describe('Input', () => {
  describe('rendering', () => {
    it('renders an input element', () => {
      render(<Input />);
      expect(screen.getByRole('textbox')).toBeInTheDocument();
    });

    it('renders a label associated with the input', () => {
      render(<Input label="Email" />);
      const label = screen.getByText('Email');
      expect(label.tagName).toBe('LABEL');
      expect(screen.getByLabelText('Email')).toBeInTheDocument();
    });

    it('renders no label when prop is omitted', () => {
      const { container } = render(<Input />);
      expect(container.querySelector('label')).not.toBeInTheDocument();
    });

    it('renders an asterisk on the label when required', () => {
      render(<Input label="Email" required />);
      expect(screen.getByLabelText('Email')).toBeRequired();
      expect(screen.getByText('Email').className).toMatch(/labelRequired/);
    });

    it('accepts a placeholder', () => {
      render(<Input placeholder="Enter your name" />);
      expect(screen.getByPlaceholderText('Enter your name')).toBeInTheDocument();
    });

    it('accepts custom type attribute', () => {
      render(<Input type="email" />);
      expect(screen.getByRole('textbox')).toHaveAttribute('type', 'email');
    });

    it('accepts aria-label when no visible label is provided', () => {
      render(<Input aria-label="Search products" />);
      expect(screen.getByRole('textbox', { name: /search products/i })).toBeInTheDocument();
    });

    it('can be disabled', () => {
      render(<Input disabled />);
      expect(screen.getByRole('textbox')).toBeDisabled();
    });

    it('accepts min/max on number inputs', () => {
      render(<Input type="number" min="1" max="100" />);
      const input = screen.getByRole('spinbutton');
      expect(input).toHaveAttribute('min', '1');
      expect(input).toHaveAttribute('max', '100');
    });
  });

  describe('variants', () => {
    it('applies default variant by default', () => {
      render(<Input />);
      expect(screen.getByRole('textbox').className).toMatch(/inputDefault/);
    });

    it('applies subtle variant', () => {
      render(<Input variant="subtle" />);
      expect(screen.getByRole('textbox').className).toMatch(/inputSubtle/);
    });

    it('applies error variant when error prop is set', () => {
      render(<Input error="Required" />);
      expect(screen.getByRole('textbox').className).toMatch(/inputError/);
    });

    it('error variant overrides an explicit variant prop', () => {
      render(<Input variant="subtle" error="Required" />);
      expect(screen.getByRole('textbox').className).not.toMatch(/inputSubtle/);
      expect(screen.getByRole('textbox').className).toMatch(/inputError/);
    });
  });

  describe('icon support', () => {
    it('adds icon padding class when icon is provided', () => {
      render(<Input icon={<EyeIcon />} />);
      expect(screen.getByRole('textbox').className).toMatch(/inputWithIcon/);
    });

    it('adds trailing padding class when trailingElement is provided', () => {
      render(<Input trailingElement={<EyeIcon />} />);
      expect(screen.getByRole('textbox').className).toMatch(/inputWithTrailing/);
    });

    it('renders the trailing element in the DOM', () => {
      render(<Input trailingElement={<button type="button">toggle</button>} />);
      expect(screen.getByRole('button', { name: 'toggle' })).toBeInTheDocument();
    });

    it('combines icon and error classes', () => {
      render(<Input icon={<EyeIcon />} error="Invalid" />);
      const input = screen.getByRole('textbox');
      expect(input.className).toMatch(/inputError/);
      expect(input.className).toMatch(/inputWithIcon/);
    });
  });

  describe('error state', () => {
    it('displays the error message', () => {
      render(<Input error="Invalid email" />);
      expect(screen.getByRole('alert')).toHaveTextContent('Invalid email');
    });

    it('displays no error message when error prop is omitted', () => {
      render(<Input />);
      expect(screen.queryByRole('alert')).not.toBeInTheDocument();
    });

    it('sets aria-invalid on the input when error is present', () => {
      render(<Input error="Required" />);
      expect(screen.getByRole('textbox')).toHaveAttribute('aria-invalid', 'true');
    });

    it('does not set aria-invalid when there is no error', () => {
      render(<Input />);
      expect(screen.getByRole('textbox')).not.toHaveAttribute('aria-invalid');
    });

    it('links the input to the error message via aria-describedby', () => {
      render(<Input error="Required" />);
      const input = screen.getByRole('textbox');
      const errorEl = screen.getByRole('alert');
      expect(input).toHaveAttribute('aria-describedby', errorEl.id);
    });

    it('does not set aria-describedby when there is no error', () => {
      render(<Input />);
      expect(screen.getByRole('textbox')).not.toHaveAttribute('aria-describedby');
    });
  });

  describe('behaviour', () => {
    it('calls onChange on every keystroke', async () => {
      const handleChange = vi.fn();
      const user = userEvent.setup();
      render(<Input onChange={handleChange} />);
      await user.type(screen.getByRole('textbox'), 'abc');
      expect(handleChange).toHaveBeenCalledTimes(3);
    });

    it('applies a custom className to the input element', () => {
      render(<Input className="custom-class" />);
      expect(screen.getByRole('textbox')).toHaveClass('custom-class');
    });
  });
});
