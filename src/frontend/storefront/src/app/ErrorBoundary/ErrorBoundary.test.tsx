import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { ErrorBoundary } from './ErrorBoundary';

vi.mock('@/shared/lib/utils/logger', () => ({
  logger: { error: vi.fn() },
}));

vi.mock('@/shared/lib/utils/telemetry', () => ({
  telemetry: { track: vi.fn() },
}));

describe('ErrorBoundary', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.stubGlobal('location', { reload: vi.fn() });
  });

  it('renders children when no error occurs', () => {
    render(
      <ErrorBoundary>
        <div data-testid="child">Child Content</div>
      </ErrorBoundary>
    );
    expect(screen.getByTestId('child')).toBeInTheDocument();
  });

  it('renders error UI when child throws an error', () => {
    const ErrorThrowingComponent = () => {
      throw new Error('Test error');
    };
    render(
      <ErrorBoundary>
        <ErrorThrowingComponent />
      </ErrorBoundary>
    );
    expect(screen.getByText('Something went wrong')).toBeInTheDocument();
  });

  it('shows error details when summary is clicked', async () => {
    const user = userEvent.setup();
    const ErrorThrowingComponent = () => {
      throw new Error('Test error message');
    };
    render(
      <ErrorBoundary>
        <ErrorThrowingComponent />
      </ErrorBoundary>
    );
    await user.click(screen.getByText('Error details'));
    expect(screen.getByText('Test error message')).toBeInTheDocument();
  });

  it('has a refresh button that reloads the page', async () => {
    const user = userEvent.setup();
    const ErrorThrowingComponent = () => {
      throw new Error('Test error');
    };
    render(
      <ErrorBoundary>
        <ErrorThrowingComponent />
      </ErrorBoundary>
    );
    await user.click(screen.getByRole('button', { name: /Refresh Page/i }));
    expect(location.reload).toHaveBeenCalled();
  });
});
