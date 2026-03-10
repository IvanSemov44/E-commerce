import { describe, it } from 'vitest';
import { render } from '@testing-library/react';
import QueryRendererSkeleton from './QueryRendererSkeleton';

describe('QueryRendererSkeleton', () => {
  it('renders without crashing', () => {
    render(<QueryRendererSkeleton />);
  });
});
