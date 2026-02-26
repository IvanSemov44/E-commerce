import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import ProductInfo from '../../pages/components/ProductDetail/ProductInfo';

describe('ProductInfo', () => {
  const defaultProps = {
    name: 'Test Product',
    description: 'This is a test product description',
    averageRating: 4.5,
    reviewCount: 100,
    price: 29.99,
    compareAtPrice: undefined,
  };

  it('renders product name', () => {
    render(<ProductInfo {...defaultProps} />);
    expect(screen.getByText('Test Product')).toBeInTheDocument();
  });

  it('renders product description', () => {
    render(<ProductInfo {...defaultProps} />);
    expect(screen.getByText('This is a test product description')).toBeInTheDocument();
  });

  it('renders product price formatted correctly', () => {
    render(<ProductInfo {...defaultProps} price={29.99} />);
    expect(screen.getByText('$29.99')).toBeInTheDocument();
  });

  it('renders compare at price when provided', () => {
    render(<ProductInfo {...defaultProps} price={29.99} compareAtPrice={39.99} />);
    expect(screen.getByText('$39.99')).toBeInTheDocument();
  });

  it('does not render compare at price when not provided', () => {
    render(<ProductInfo {...defaultProps} compareAtPrice={undefined} />);
    // Should only have one price element
    const prices = screen.getAllByText(/\$/);
    expect(prices.length).toBe(1);
  });

  it('renders rating value', () => {
    render(<ProductInfo {...defaultProps} averageRating={4.5} />);
    expect(screen.getByText('4.5')).toBeInTheDocument();
  });

  it('renders review count', () => {
    render(<ProductInfo {...defaultProps} reviewCount={100} />);
    expect(screen.getByText('(100 reviews)')).toBeInTheDocument();
  });

  it('handles zero reviews', () => {
    render(<ProductInfo {...defaultProps} reviewCount={0} />);
    expect(screen.getByText('(0 reviews)')).toBeInTheDocument();
  });
});
