import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import AppProviders from './AppProviders';

describe('AppProviders', () => {
  it('renders children within all providers', () => {
    render(
      <AppProviders>
        <div data-testid="test-child">Test Child</div>
      </AppProviders>
    );
    expect(screen.getByTestId('test-child')).toBeInTheDocument();
  });
});
