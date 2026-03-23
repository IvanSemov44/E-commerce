import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { BrowserRouter } from 'react-router';
import { ErrorPage } from './ErrorPage';

const renderErrorPage = (props = {}) => {
  const defaultProps = {
    error: null,
    isDevelopment: false,
    onReset: vi.fn(),
  };
  return render(
    <BrowserRouter>
      <ErrorPage {...defaultProps} {...props} />
    </BrowserRouter>
  );
};

describe('ErrorPage', () => {
  beforeEach(() => {
    vi.stubGlobal('location', { reload: vi.fn() });
  });

  it('renders error page with default content', () => {
    renderErrorPage();
    expect(screen.getByText(/Oops! Something went wrong/)).toBeInTheDocument();
    expect(screen.getByText(/Go to Home/)).toBeInTheDocument();
    expect(screen.getByText(/Refresh Page/)).toBeInTheDocument();
  });

  it('shows error details in development mode', () => {
    const error = new Error('Test error message');
    renderErrorPage({ error, isDevelopment: true });
    expect(screen.getByText(/Error Details/)).toBeInTheDocument();
    expect(screen.getByText(/Test error message/)).toBeInTheDocument();
  });

  it('does not show error details in production mode', () => {
    const error = new Error('Test error message');
    renderErrorPage({ error, isDevelopment: false });
    expect(screen.queryByText(/Error Details/)).not.toBeInTheDocument();
  });

  it('calls onReset and navigates to home when Go to Home is clicked', async () => {
    const user = userEvent.setup();
    const onReset = vi.fn();
    renderErrorPage({ onReset });
    await user.click(screen.getByRole('button', { name: /Go to Home/i }));
    expect(onReset).toHaveBeenCalled();
  });

  it('reloads page when Refresh Page is clicked', async () => {
    const user = userEvent.setup();
    renderErrorPage();
    await user.click(screen.getByRole('button', { name: /Refresh Page/i }));
    expect(location.reload).toHaveBeenCalled();
  });

  it('displays support contact link', () => {
    renderErrorPage();
    const link = screen.getByRole('link', { name: /contact support/i });
    expect(link).toHaveAttribute('href', 'mailto:support@example.com');
  });
});
