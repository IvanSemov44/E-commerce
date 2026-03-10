import { describe, it, expect } from 'vitest';
import { render } from '@testing-library/react';
import SkeletonLabelRow from './SkeletonLabelRow';

const items = [
  { width: '40%', height: 16 },
  { width: '25%', height: 16 },
];

describe('SkeletonLabelRow', () => {
  it('renders correct number of skeletons', () => {
    const { container } = render(<SkeletonLabelRow items={items} />);
    const skeletons = container.querySelectorAll('span[class*="skeleton"]');
    expect(skeletons).toHaveLength(2);
  });

  it('renders 3 items when given 3', () => {
    const { container } = render(
      <SkeletonLabelRow items={[...items, { width: '20%', height: 14 }]} between={false} wrap />
    );
    const skeletons = container.querySelectorAll('span[class*="skeleton"]');
    expect(skeletons).toHaveLength(3);
  });
});
