import { describe, it } from 'vitest';
import { render } from '@testing-library/react';
import AppShellSkeleton from './AppShellSkeleton';

describe('AppShellSkeleton', () => {
  it('renders without crashing', () => {
    render(<AppShellSkeleton />);
  });
});
