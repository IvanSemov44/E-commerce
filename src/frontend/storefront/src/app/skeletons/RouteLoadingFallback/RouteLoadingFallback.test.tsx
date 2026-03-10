import { describe, it } from 'vitest';
import { render } from '@testing-library/react';
import RouteLoadingFallback from './RouteLoadingFallback';

describe('RouteLoadingFallback', () => {
  it('renders without crashing', () => {
    render(<RouteLoadingFallback />);
  });
});
