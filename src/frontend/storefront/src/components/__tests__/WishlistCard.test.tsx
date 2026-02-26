import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import { BrowserRouter } from 'react-router-dom';
import WishlistCard from '../../pages/components/Wishlist/WishlistCard';

const renderWithRouter = (component: React.ReactElement) => {
  return render(<BrowserRouter>{component}</BrowserRouter>);
};

describe('WishlistCard', () => {
  const mockProduct = {
    id: '1',
    name: 'Test Product',
    slug: 'test-product',
    price: 29.99,
    compareAtPrice: 39.99,
    images: [{ url: 'test.jpg' }],
    stockQuantity: 10,
    isAvailable: true,
  };

  const defaultProps = {
    product: mockProduct,
    onRemove: vi.fn(),
  };

  it('renders product name', () => {
    renderWithRouter(<WishlistCard {...defaultProps} />);
    expect(screen.getByText('Test Product')).toBeInTheDocument();
  });

  it('renders price correctly', () => {
    renderWithRouter(<WishlistCard {...defaultProps} />);
    expect(screen.getByText('$29.99')).toBeInTheDocument();
  });

  it('renders compare at price when discounted', () => {
    renderWithRouter(<WishlistCard {...defaultProps} />);
    expect(screen.getByText('$39.99')).toBeInTheDocument();
  });

  it('renders discount badge when discounted', () => {
    renderWithRouter(<WishlistCard {...defaultProps} />);
    expect(screen.getByText(/-25%/)).toBeInTheDocument();
  });

  it('renders In Stock when in stock', () => {
    renderWithRouter(<WishlistCard {...defaultProps} product={{ ...mockProduct, stockQuantity: 10 }} />);
    expect(screen.getByText('In Stock')).toBeInTheDocument();
  });

  it('renders Out of Stock when out of stock', () => {
    renderWithRouter(<WishlistCard {...defaultProps} product={{ ...mockProduct, stockQuantity: 0 }} />);
    expect(screen.getByText('Out of Stock')).toBeInTheDocument();
  });

  it('renders low stock message when low stock', () => {
    renderWithRouter(<WishlistCard {...defaultProps} product={{ ...mockProduct, stockQuantity: 3 }} />);
    expect(screen.getByText('Only 3 left')).toBeInTheDocument();
  });

  it('calls onRemove when remove button is clicked', () => {
    const onRemove = vi.fn();
    renderWithRouter(<WishlistCard {...defaultProps} onRemove={onRemove} />);
    
    const removeButton = screen.getByRole('button', { name: /remove from wishlist/i });
    fireEvent.click(removeButton);
    expect(onRemove).toHaveBeenCalledWith('1');
  });

  it('renders add to cart button when in stock', () => {
    renderWithRouter(<WishlistCard {...defaultProps} />);
    expect(screen.getByRole('button', { name: /add/i })).toBeInTheDocument();
  });

  it('does not render add to cart button when out of stock', () => {
    renderWithRouter(<WishlistCard {...defaultProps} product={{ ...mockProduct, stockQuantity: 0 }} />);
    expect(screen.queryByRole('button', { name: /add/i })).not.toBeInTheDocument();
  });
});
