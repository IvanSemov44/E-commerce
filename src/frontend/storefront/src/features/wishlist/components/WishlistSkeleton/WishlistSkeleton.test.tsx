import { describe, it, expect } from 'vitest';
import { render } from '@testing-library/react';
import { WishlistSkeleton } from './WishlistSkeleton';

describe('WishlistSkeleton', () => {
  it('renders default 8 skeleton cards', () => {
    const { container } = render(<WishlistSkeleton />);
    // SkeletonCard renders a div with a "card" CSS module class
    expect(container.querySelectorAll('[class*="card"]')).toHaveLength(8);
  });

  it('renders the specified count of skeleton cards', () => {
    const { container } = render(<WishlistSkeleton count={3} />);
    expect(container.querySelectorAll('[class*="card"]')).toHaveLength(3);
  });

  it('renders grid wrapper with grid class', () => {
    const { container } = render(<WishlistSkeleton />);
    expect(container.querySelector('[class*="grid"]')).toBeInTheDocument();
  });

  it('renders nothing inside grid when count is 0', () => {
    const { container } = render(<WishlistSkeleton count={0} />);
    expect(container.querySelectorAll('[class*="card"]')).toHaveLength(0);
    expect(container.querySelector('[class*="grid"]')).toBeInTheDocument();
  });
});
