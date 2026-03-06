import { screen } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import { renderWithProviders as render } from '@/shared/lib/test/test-utils';
import ProductGrid from '../ProductGrid';
import type { Product } from '@/shared/types';

const mockProducts: Product[] = [
  {
    id: '1',
    name: 'Product 1',
    slug: 'product-1',
    description: 'Description 1',
    price: 29.99,
    compareAtPrice: 39.99,
    stockQuantity: 10,
    images: [{ id: 'img-1', url: 'http://example.com/image1.jpg', altText: 'Product 1', isPrimary: true }],
    averageRating: 4.5,
    reviewCount: 10,
    isFeatured: true,
  },
  {
    id: '2',
    name: 'Product 2',
    slug: 'product-2',
    description: 'Description 2',
    price: 49.99,
    compareAtPrice: undefined,
    stockQuantity: 5,
    images: [{ id: 'img-2', url: 'http://example.com/image2.jpg', altText: 'Product 2', isPrimary: true }],
    averageRating: 4.0,
    reviewCount: 5,
    isFeatured: false,
  },
];

const defaultProps = {
  products: mockProducts,
  totalCount: 2,
  currentPage: 1,
  pageSize: 10,
  onPageChange: vi.fn(),
};

describe('ProductGrid', () => {
  it('renders results count', () => {
    render(<ProductGrid {...defaultProps} />);

    const resultsCount = screen.getByText(/showing/i).parentElement;
    expect(resultsCount).toBeInTheDocument();
    expect(resultsCount).toHaveTextContent('2');
    expect(resultsCount).toHaveTextContent('products');
  });

  it('renders all products', () => {
    render(<ProductGrid {...defaultProps} />);

    expect(screen.getByText('Product 1')).toBeInTheDocument();
    expect(screen.getByText('Product 2')).toBeInTheDocument();
  });

  it('shows correct count when products differ from total', () => {
    render(<ProductGrid {...defaultProps} products={mockProducts} totalCount={20} />);

    const resultsCount = screen.getByText(/showing/i).parentElement;
    expect(resultsCount).toBeInTheDocument();
    expect(resultsCount).toHaveTextContent('2');
    expect(resultsCount).toHaveTextContent('20');
  });

  it('renders empty grid when no products', () => {
    render(<ProductGrid {...defaultProps} products={[]} totalCount={0} />);

    expect(screen.getByText(/showing/i)).toBeInTheDocument();
    expect(screen.queryByText('Product 1')).not.toBeInTheDocument();
  });

  it('passes pagination props to PaginatedView', () => {
    const onPageChange = vi.fn();
    render(<ProductGrid {...defaultProps} currentPage={2} onPageChange={onPageChange} />);

    expect(screen.getByText(/showing/i)).toBeInTheDocument();
  });
});
