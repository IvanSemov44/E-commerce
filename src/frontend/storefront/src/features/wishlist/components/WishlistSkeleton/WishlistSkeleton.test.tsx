import { describe, it } from 'vitest';
import { render } from '@testing-library/react';
import WishlistSkeleton from './WishlistSkeleton';

describe('WishlistSkeleton', () => {
  it('renders without crashing', () => {
    render(<WishlistSkeleton />);
  });
});
