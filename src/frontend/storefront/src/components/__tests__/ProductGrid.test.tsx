import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import ProductGrid from '../../pages/components/Products/ProductGrid';
import type { Product } from '../../types';

describe('ProductGrid', () => {
  const mockProducts: Product[] = [
    {
      id: '1',
      name: 'Product 1',
      slug: 'product-1',
      price: 29.99,
      images: [{ id: '1', url: 'img1.jpg', isPrimary: true }],
      stockQuantity: 10,
      averageRating: 4.5,
      reviewCount: 20,
    },
    {
      id: '2',
      name: 'Product 2',
      slug: 'product-2',
      price: 49.99,
      compareAtPrice: 59.99,
      images: [{ id: '2', url: 'img2.jpg', isPrimary: true }],
      stockQuantity: 5,
      averageRating: 5,
      reviewCount: 100,
    },
  ];

  const defaultProps = {
    products: mockProducts,
    totalCount: 25,
    currentPage: 1,
    pageSize: 12,
    onPageChange: vi.fn(),
  };

  it('renders results count', () => {
    render(<ProductGrid {...defaultProps} />);
    expect(screen.getByText(/Showing/)).toBeInTheDocument();
    expect(screen.getByText('2')).toBeInTheDocument();
    expect(screen.getByText('25')).toBeInTheDocument();
  });

  it('displays correct products count', () => {
    render(<ProductGrid {...defaultProps} />);
    expect(screen.getByText('2')).toBeInTheDocument(); // products.length
  });

  it('displays total count', () => {
    render(<ProductGrid {...defaultProps} />);
    expect(screen.getByText('25')).toBeInTheDocument(); // totalCount
  });

  it('renders product names', () => {
    render(<ProductGrid {...defaultProps} />);
    expect(screen.getByText('Product 1')).toBeInTheDocument();
    expect(screen.getByText('Product 2')).toBeInTheDocument();
  });

  it('handles empty products array', () => {
    render(<ProductGrid {...defaultProps} products={[]} totalCount={0} />);
    expect(screen.getByText(/Showing/)).toBeInTheDocument();
  });

  it('handles different page sizes', () => {
    render(<ProductGrid {...defaultProps} pageSize={24} />);
    expect(screen.getByText('24')).toBeInTheDocument();
  });
});
