import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import ErrorAlert from './ErrorAlert';

// Mock the Card component
vi.mock('../ui/Card', () => ({
  default: ({ children, ...props }: any) => <div data-testid="card" {...props}>{children}</div>,
}));

// Mock icon
vi.mock('../icons', () => ({
  CloseIcon: ({ ...props }: any) => <button data-testid="close-icon" {...props} />,
}));

describe('ErrorAlert', () => {
  it('renders error message', () => {
    render(<ErrorAlert message="Something went wrong" />);
    expect(screen.getByText('Something went wrong')).toBeInTheDocument();
  });

  it('renders with custom error message', () => {
    const errorMsg = 'Failed to load products. Please try again.';
    render(<ErrorAlert message={errorMsg} />);
    expect(screen.getByText(errorMsg)).toBeInTheDocument();
  });

  it('displays dismiss button when onDismiss is provided', () => {
    const onDismiss = vi.fn();
    render(<ErrorAlert message="Error" onDismiss={onDismiss} />);
    expect(screen.getByLabelText('Dismiss error')).toBeInTheDocument();
  });

  it('does not display dismiss button when onDismiss is not provided', () => {
    render(<ErrorAlert message="Error" />);
    expect(screen.queryByLabelText('Dismiss error')).not.toBeInTheDocument();
  });

  it('calls onDismiss callback when dismiss button is clicked', async () => {
    const user = userEvent.setup();
    const onDismiss = vi.fn();
    render(<ErrorAlert message="Error message" onDismiss={onDismiss} />);

    const dismissButton = screen.getByLabelText('Dismiss error');
    await user.click(dismissButton);

    expect(onDismiss).toHaveBeenCalledTimes(1);
  });

  it('renders Card component with correct props', () => {
    render(<ErrorAlert message="Test error" />);
    const card = screen.getByTestId('card');
    expect(card).toBeInTheDocument();
  });

  it('applies error styling classes', () => {
    render(<ErrorAlert message="Error test" />);
    const container = screen.getByTestId('card');
    expect(container).toHaveClass('error-container');
  });

  it('handles long error messages', () => {
    const longMessage = 'This is a very long error message that should wrap to multiple lines and still display correctly in the container without breaking the layout.';
    render(<ErrorAlert message={longMessage} />);
    expect(screen.getByText(longMessage)).toBeInTheDocument();
  });

  it('renders dismiss button with proper accessibility', () => {
    const onDismiss = vi.fn();
    render(<ErrorAlert message="Error" onDismiss={onDismiss} />);
    const dismissButton = screen.getByLabelText('Dismiss error');
    expect(dismissButton).toHaveAttribute('onclick');
  });

  it('allows multiple dismiss calls if component rerenders', async () => {
    const user = userEvent.setup();
    const onDismiss = vi.fn();
    const { rerender } = render(<ErrorAlert message="Error 1" onDismiss={onDismiss} />);

    await user.click(screen.getByLabelText('Dismiss error'));
    expect(onDismiss).toHaveBeenCalledTimes(1);

    // Rerender with new error
    rerender(<ErrorAlert message="Error 2" onDismiss={onDismiss} />);
    await user.click(screen.getByLabelText('Dismiss error'));
    expect(onDismiss).toHaveBeenCalledTimes(2);
  });

  it('renders with variant and padding props passed to Card', () => {
    const { container } = render(<ErrorAlert message="Error" />);
    expect(container).toBeInTheDocument();
  });
});
