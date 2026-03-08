import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import ProductInfo from './ProductInfo';

describe('ProductInfo', () => {
  const defaultProps = {
    name: 'Premium Wireless Headphones',
    description: 'High-quality audio with noise cancellation',
    averageRating: 4.5,
    reviewCount: 128,
    price: 199.99,
    compareAtPrice: 249.99,
  };

  it('should render product name', () => {
    render(<ProductInfo {...defaultProps} />);
    expect(screen.getByRole('heading', { level: 1 })).toHaveTextContent(
      'Premium Wireless Headphones'
    );
  });

  it('should display average rating and review count', () => {
    render(<ProductInfo {...defaultProps} />);
    expect(screen.getByText('4.5')).toBeInTheDocument();
    expect(screen.getByText('(128 reviews)')).toBeInTheDocument();
  });

  it('should format price with two decimal places', () => {
    render(<ProductInfo {...defaultProps} />);
    expect(screen.getByText('$199.99')).toBeInTheDocument();
  });

  it('should show compare-at price when provided', () => {
    render(<ProductInfo {...defaultProps} />);
    expect(screen.getByText('$249.99')).toBeInTheDocument();
  });

  it('should not show compare-at price when not provided', () => {
    const propsWithoutCompare = { ...defaultProps, compareAtPrice: undefined };
    render(<ProductInfo {...propsWithoutCompare} />);
    expect(screen.queryByText('$249.99')).not.toBeInTheDocument();
  });

  it('should render description when provided', () => {
    render(<ProductInfo {...defaultProps} />);
    expect(screen.getByText('High-quality audio with noise cancellation')).toBeInTheDocument();
  });

  it('should not render description when undefined', () => {
    const propsWithoutDesc = { ...defaultProps, description: undefined };
    render(<ProductInfo {...propsWithoutDesc} description={undefined} />);
    expect(
      screen.queryByText('High-quality audio with noise cancellation')
    ).not.toBeInTheDocument();
  });

  it('should handle zero review count', () => {
    render(<ProductInfo {...defaultProps} reviewCount={0} />);
    expect(screen.getByText('(0 reviews)')).toBeInTheDocument();
  });

  it('should handle zero rating', () => {
    render(<ProductInfo {...defaultProps} averageRating={0} />);
    expect(screen.getByText('0')).toBeInTheDocument();
  });

  it('should render rating star icon', () => {
    render(<ProductInfo {...defaultProps} />);
    expect(screen.getByText('★')).toBeInTheDocument();
  });
});
