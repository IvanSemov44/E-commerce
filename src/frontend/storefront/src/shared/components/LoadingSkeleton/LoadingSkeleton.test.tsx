import { describe, it, expect } from 'vitest';
import { render } from '@testing-library/react';
import LoadingSkeleton from './LoadingSkeleton';

describe('LoadingSkeleton', () => {
  it('renders one card skeleton by default', () => {
    const { container } = render(<LoadingSkeleton />);
    const animated = container.querySelectorAll('.animate-pulse');
    expect(animated).toHaveLength(1);
  });

  it('renders multiple card skeletons by count', () => {
    const { container } = render(<LoadingSkeleton count={3} type="card" />);
    const animated = container.querySelectorAll('.animate-pulse');
    expect(animated).toHaveLength(3);
  });

  it('renders text skeletons for text type', () => {
    const { container } = render(<LoadingSkeleton count={4} type="text" />);
    const animated = container.querySelectorAll('.animate-pulse');
    expect(animated).toHaveLength(4);
  });

  it('renders image skeleton for image type', () => {
    const { container } = render(<LoadingSkeleton type="image" />);
    const animated = container.querySelectorAll('.animate-pulse');
    expect(animated).toHaveLength(1);
  });

  it('renders image type as single element regardless of count', () => {
    const { container } = render(<LoadingSkeleton type="image" count={5} />);
    const animated = container.querySelectorAll('.animate-pulse');
    expect(animated).toHaveLength(1);
  });
});
