import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import ProductActions from '../../pages/components/ProductDetail/ProductActions';

describe('ProductActions', () => {
  const defaultProps = {
    stockQuantity: 10,
    lowStockThreshold: 5,
    quantity: 1,
    cartItem: undefined,
    addedToCart: false,
    addingToCartBackend: false,
    cartError: null,
    isAuthenticated: false,
    isInWishlist: undefined,
    addingToWishlist: false,
    removingFromWishlist: false,
    onQuantityChange: vi.fn(),
    onAddToCart: vi.fn(),
    onToggleWishlist: vi.fn(),
    onDismissError: vi.fn(),
  };

  it('renders stock status when in stock', () => {
    render(<ProductActions {...defaultProps} stockQuantity={10} />);
    expect(screen.getByText('10 in stock')).toBeInTheDocument();
  });

  it('renders out of stock when quantity is 0', () => {
    render(<ProductActions {...defaultProps} stockQuantity={0} />);
    expect(screen.getByText('Out of stock')).toBeInTheDocument();
  });

  it('renders low stock warning when near threshold', () => {
    render(<ProductActions {...defaultProps} stockQuantity={3} lowStockThreshold={5} />);
    expect(screen.getByText(/Only 3 left!/)).toBeInTheDocument();
  });

  it('does not render low stock warning when stock is high', () => {
    render(<ProductActions {...defaultProps} stockQuantity={10} lowStockThreshold={5} />);
    expect(screen.queryByText(/Only/)).not.toBeInTheDocument();
  });

  it('renders quantity label', () => {
    render(<ProductActions {...defaultProps} />);
    expect(screen.getByText('Quantity:')).toBeInTheDocument();
  });

  it('renders Add to Cart button', () => {
    render(<ProductActions {...defaultProps} />);
    expect(screen.getByRole('button', { name: /add to cart/i })).toBeInTheDocument();
  });

  it('renders Out of Stock button when out of stock', () => {
    render(<ProductActions {...defaultProps} stockQuantity={0} />);
    expect(screen.getByRole('button', { name: /out of stock/i })).toBeInTheDocument();
  });

  it('renders Added to Cart button when added', () => {
    render(<ProductActions {...defaultProps} addedToCart={true} />);
    expect(screen.getByRole('button', { name: /added to cart/i })).toBeInTheDocument();
  });

  it('calls onAddToCart when button is clicked', () => {
    const onAddToCart = vi.fn();
    render(<ProductActions {...defaultProps} onAddToCart={onAddToCart} />);
    
    fireEvent.click(screen.getByRole('button', { name: /add to cart/i }));
    expect(onAddToCart).toHaveBeenCalledTimes(1);
  });

  it('renders wishlist button when authenticated', () => {
    render(<ProductActions {...defaultProps} isAuthenticated={true} />);
    expect(screen.getByRole('button', { name: /add to wishlist/i })).toBeInTheDocument();
  });

  it('renders In Wishlist when already in wishlist', () => {
    render(<ProductActions {...defaultProps} isAuthenticated={true} isInWishlist={true} />);
    expect(screen.getByRole('button', { name: /in wishlist/i })).toBeInTheDocument();
  });

  it('does not render wishlist button when not authenticated', () => {
    render(<ProductActions {...defaultProps} isAuthenticated={false} />);
    expect(screen.queryByRole('button', { name: /wishlist/i })).not.toBeInTheDocument();
  });

  it('renders error message when cartError is present', () => {
    render(<ProductActions {...defaultProps} cartError="Failed to add to cart" />);
    expect(screen.getByText('Failed to add to cart')).toBeInTheDocument();
  });

  it('calls onQuantityChange when minus button is clicked', () => {
    const onQuantityChange = vi.fn();
    render(<ProductActions {...defaultProps} quantity={2} onQuantityChange={onQuantityChange} />);
    
    const minusButton = document.querySelector('button') as HTMLButtonElement;
    // Find the minus button (first button in quantity controls)
    const buttons = screen.getAllByRole('button');
    fireEvent.click(buttons[0]); // First button is minus
    expect(onQuantityChange).toHaveBeenCalled();
  });
});
