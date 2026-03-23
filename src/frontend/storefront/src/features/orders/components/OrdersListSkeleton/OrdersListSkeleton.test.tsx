import { describe, it } from 'vitest';
import { render } from '@testing-library/react';
import { OrdersListSkeleton } from './OrdersListSkeleton';

describe('OrdersListSkeleton', () => {
  it('renders without crashing', () => {
    render(<OrdersListSkeleton />);
  });
});
