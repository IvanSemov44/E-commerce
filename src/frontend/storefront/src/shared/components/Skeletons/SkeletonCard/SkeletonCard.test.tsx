import { describe, it, expect } from 'vitest';
import { render } from '@testing-library/react';
import SkeletonCard from './SkeletonCard';

describe('SkeletonCard', () => {
  it('renders image skeleton at given height', () => {
    const { container } = render(<SkeletonCard imageHeight={200} />);
    const skeletons = container.querySelectorAll('span[class*="skeleton"]');
    expect(skeletons.length).toBeGreaterThanOrEqual(1);
  });

  it('renders default 2 text lines', () => {
    const { container } = render(<SkeletonCard imageHeight={200} />);
    // image + 2 default lines = 3 skeletons
    const skeletons = container.querySelectorAll('span[class*="skeleton"]');
    expect(skeletons).toHaveLength(3);
  });

  it('renders custom lines', () => {
    const { container } = render(
      <SkeletonCard imageHeight={200} lines={[{ width: '60%', height: 18 }]} />
    );
    // image + 1 custom line = 2 skeletons
    const skeletons = container.querySelectorAll('span[class*="skeleton"]');
    expect(skeletons).toHaveLength(2);
  });

  it('renders children', () => {
    const { getByTestId } = render(
      <SkeletonCard imageHeight={200}>
        <div data-testid="child" />
      </SkeletonCard>
    );
    expect(getByTestId('child')).toBeInTheDocument();
  });
});
