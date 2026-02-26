import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import CartItemList from '../../pages/components/Cart/CartItemList';

describe('CartItemList', () => {
  const mockItems = [
    {
      id: '1',
      name: 'Product 1',
      slug: 'product-1',
      price: 29.99,
      quantity: 2,
      maxStock: 10,
      image: 'img1.jpg',
    },
    {
      id: '2',
      name: 'Product 2',
      slug: 'product-2',
      price: 15.00,
      quantity: 1,
      maxStock: 5,
      image: 'img2.jpg',
    },
  ];

  const defaultProps = {
    items: mockItems,
    onUpdateQuantity: vi.fn(),
    onRemove: vi.fn(),
  };

  it('renders Items title with count', () => {
    render(<CartItemList {...defaultProps} />);
    expect(screen.getByText(/Items/)).toBeInTheDocument();
  });

  it('renders correct product count (plural)', () => {
    render(<CartItemList {...defaultProps} />);
    expect(screen.getByText('2 products')).toBeInTheDocument();
  });

  it('renders correct product count (singular)', () => {
    render(<CartItemList {...defaultProps} items={[mockItems[0]]} />);
    expect(screen.getByText('1 product')).toBeInTheDocument();
  });

  it('renders all cart items', () => {
    render(<CartItemList {...defaultProps} />);
    expect(screen.getByText('Product 1')).toBeInTheDocument();
    expect(screen.getByText('Product 2')).toBeInTheDocument();
  });

  it('handles empty items array', () => {
    render(<CartItemList {...defaultProps} items={[]} />);
    expect(screen.getByText('0 products')).toBeInTheDocument();
  });
});
