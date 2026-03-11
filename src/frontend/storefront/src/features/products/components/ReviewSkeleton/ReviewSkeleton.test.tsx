import { describe, it, expect } from 'vitest';
import { render } from '@testing-library/react';
import ReviewSkeleton from './ReviewSkeleton';

describe('ReviewSkeleton', () => {
  it('renders grid container', () => {
    const { container } = render(<ReviewSkeleton />);
    const grid = container.querySelector('[class*="grid"]');
    expect(grid).toBeInTheDocument();
  });

  it('renders three review card skeletons by default', () => {
    const { container } = render(<ReviewSkeleton />);
    const grid = container.querySelector('[class*="grid"]');
    const cards = grid?.querySelectorAll('[class*="card"]');
    expect(cards).toHaveLength(3);
  });

  it('renders multiple skeleton elements', () => {
    const { container } = render(<ReviewSkeleton />);
    const skeletons = container.querySelectorAll('span[class*="skeleton"]');
    expect(skeletons.length).toBeGreaterThan(10);
  });

  it('renders skeleton label rows', () => {
    const { container } = render(<ReviewSkeleton />);
    const labelRows = container.querySelectorAll('[class*="skeletonLabelRow"]');
    expect(labelRows.length).toBeGreaterThan(0);
  });

  it('all skeletons have proper aria attributes', () => {
    const { container } = render(<ReviewSkeleton />);
    const skeletons = container.querySelectorAll('span[class*="skeleton"]');
    skeletons.forEach((skeleton) => {
      expect(skeleton).toHaveAttribute('aria-hidden', 'true');
    });
  });
});
