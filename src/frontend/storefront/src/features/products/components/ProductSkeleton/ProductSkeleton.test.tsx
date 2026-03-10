import { describe, it, expect } from 'vitest';
import { render } from '@testing-library/react';
import ProductSkeleton from './ProductSkeleton';

describe('ProductSkeleton', () => {
  it('renders product card skeleton structure', () => {
    const { container } = render(<ProductSkeleton />);
    const productCard = container.querySelector('[class*="productCard"]');
    expect(productCard).toBeInTheDocument();
  });

  it('renders product image skeleton', () => {
    const { container } = render(<ProductSkeleton />);
    const productImage = container.querySelector('[class*="productImage"]');
    expect(productImage).toBeInTheDocument();
  });

  it('renders product info section', () => {
    const { container } = render(<ProductSkeleton />);
    const productInfo = container.querySelector('[class*="productInfo"]');
    expect(productInfo).toBeInTheDocument();
  });

  it('renders multiple skeleton elements', () => {
    const { container } = render(<ProductSkeleton />);
    const skeletons = container.querySelectorAll('span[class*="skeleton"]');
    expect(skeletons.length).toBeGreaterThanOrEqual(5);
  });

  it('renders price row', () => {
    const { container } = render(<ProductSkeleton />);
    const priceRow = container.querySelector('[class*="priceRow"]');
    expect(priceRow).toBeInTheDocument();
  });

  it('renders rating row', () => {
    const { container } = render(<ProductSkeleton />);
    const ratingRow = container.querySelector('[class*="ratingRow"]');
    expect(ratingRow).toBeInTheDocument();
  });

  it('renders add to cart button skeleton', () => {
    const { container } = render(<ProductSkeleton />);
    const button = container.querySelector('[class*="button"]');
    expect(button).toBeInTheDocument();
  });

  it('all skeletons have aria-busy attribute', () => {
    const { container } = render(<ProductSkeleton />);
    const skeletons = container.querySelectorAll('span[class*="skeleton"]');
    skeletons.forEach((skeleton) => {
      expect(skeleton).toHaveAttribute('aria-busy', 'true');
    });
  });
});
