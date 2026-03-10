import { describe, it } from 'vitest';
import { render } from '@testing-library/react';
import OrderDetailSkeleton from './OrderDetailSkeleton';

describe('OrderDetailSkeleton', () => {
  it('renders without crashing', () => {
    render(<OrderDetailSkeleton />);
  });
});
