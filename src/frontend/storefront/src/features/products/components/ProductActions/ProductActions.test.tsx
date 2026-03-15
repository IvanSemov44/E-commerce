import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, it, expect, vi } from 'vitest';
import { ProductActions } from './ProductActions';
import type { ProductActionsProps } from './ProductActions.types';

const defaultProps: ProductActionsProps = {
  stockQuantity: 10,
  lowStockThreshold: 3,
  isAuthenticated: true,
  cart: {
    quantity: 1,
    cartItem: undefined,
    addedToCart: false,
    isLoading: false,
    error: null,
  },
  wishlist: {
    isInWishlist: false,
    isAdding: false,
    isRemoving: false,
  },
  onQuantityChange: vi.fn(),
  onAddToCart: vi.fn(),
  onToggleWishlist: vi.fn(),
  onDismissError: vi.fn(),
};

describe('ProductActions', () => {
  it('displays in stock status', () => {
    render(<ProductActions {...defaultProps} />);

    expect(screen.getByText('10 in stock')).toBeInTheDocument();
  });

  it('displays low stock warning', () => {
    render(<ProductActions {...defaultProps} stockQuantity={2} />);

    expect(screen.getByText(/only 2 left/i)).toBeInTheDocument();
  });

  it('displays out of stock message', () => {
    render(<ProductActions {...defaultProps} stockQuantity={0} />);

    expect(screen.getAllByText(/out of stock/i).length).toBeGreaterThan(0);
  });

  it('renders quantity controls', () => {
    render(<ProductActions {...defaultProps} />);

    expect(screen.getByDisplayValue('1')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: '−' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: '+' })).toBeInTheDocument();
  });

  it('calls onQuantityChange when quantity changes', async () => {
    const user = userEvent.setup();
    const onQuantityChange = vi.fn();
    render(<ProductActions {...defaultProps} onQuantityChange={onQuantityChange} />);

    const increaseButton = screen.getByRole('button', { name: '+' });
    await user.click(increaseButton);

    expect(onQuantityChange).toHaveBeenCalledWith(2);
  });

  it('shows cart hint when item is in cart', () => {
    render(
      <ProductActions
        {...defaultProps}
        cart={{ ...defaultProps.cart, cartItem: { quantity: 2 } }}
      />
    );

    expect(screen.getByText(/2 in cart/i)).toBeInTheDocument();
  });

  it('renders add to cart button', () => {
    render(<ProductActions {...defaultProps} />);

    expect(screen.getByRole('button', { name: /add to cart/i })).toBeInTheDocument();
  });

  it('disables add to cart when out of stock', () => {
    render(<ProductActions {...defaultProps} stockQuantity={0} />);

    const button = screen.getByRole('button', { name: /out of stock/i });
    expect(button).toBeDisabled();
  });

  it('shows added confirmation', () => {
    render(<ProductActions {...defaultProps} cart={{ ...defaultProps.cart, addedToCart: true }} />);

    expect(screen.getByText(/✓ added to cart/i)).toBeInTheDocument();
  });

  it('renders wishlist button when authenticated', () => {
    render(<ProductActions {...defaultProps} isAuthenticated={true} />);

    expect(screen.getByRole('button', { name: /wishlist/i })).toBeInTheDocument();
  });

  it('does not render wishlist button when not authenticated', () => {
    render(<ProductActions {...defaultProps} isAuthenticated={false} />);

    expect(screen.queryByRole('button', { name: /wishlist/i })).not.toBeInTheDocument();
  });

  it('shows error message when cart error exists', () => {
    render(
      <ProductActions
        {...defaultProps}
        cart={{ ...defaultProps.cart, error: 'Failed to add to cart' }}
      />
    );

    expect(screen.getByText('Failed to add to cart')).toBeInTheDocument();
  });

  it('calls onDismissError when error is dismissed', async () => {
    const user = userEvent.setup();
    const onDismissError = vi.fn();
    render(
      <ProductActions
        {...defaultProps}
        cart={{ ...defaultProps.cart, error: 'Failed to add' }}
        onDismissError={onDismissError}
      />
    );

    const dismissButton = screen.getByRole('button', { name: /close|dismiss|×/i });
    await user.click(dismissButton);

    expect(onDismissError).toHaveBeenCalled();
  });

  it('disables increase button at max stock', () => {
    render(
      <ProductActions
        {...defaultProps}
        stockQuantity={10}
        cart={{ ...defaultProps.cart, quantity: 10 }}
      />
    );

    const increaseButton = screen.getByRole('button', { name: '+' });
    expect(increaseButton).toBeDisabled();
  });
});
