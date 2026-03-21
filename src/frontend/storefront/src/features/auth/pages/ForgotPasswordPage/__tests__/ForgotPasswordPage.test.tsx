import { describe, it, expect, vi, beforeEach } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { renderWithProviders } from '@/shared/lib/test/test-utils';
import { ForgotPasswordPage } from '../ForgotPasswordPage';

const mockForgotPassword = vi.fn();

vi.mock('@/features/auth/api/authApi', () => ({
  useForgotPasswordMutation: () => [mockForgotPassword, {}],
}));

function successResponse() {
  return {
    unwrap: vi.fn().mockResolvedValue({ success: true }),
  };
}

describe('ForgotPasswordPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockForgotPassword.mockReturnValue(successResponse());
  });

  it('renders email field and submit button', () => {
    renderWithProviders(<ForgotPasswordPage />);

    expect(screen.getByLabelText(/^email/i)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /send reset link/i })).toBeInTheDocument();
  });

  it('shows email required error on empty submit', async () => {
    const user = userEvent.setup();
    renderWithProviders(<ForgotPasswordPage />);

    await user.click(screen.getByRole('button', { name: /send reset link/i }));

    await waitFor(() => {
      expect(screen.getByText('Email is required')).toBeInTheDocument();
    });
    expect(mockForgotPassword).not.toHaveBeenCalled();
  });

  it('shows success state on successful submission', async () => {
    const user = userEvent.setup();
    renderWithProviders(<ForgotPasswordPage />);

    await user.type(screen.getByLabelText(/^email/i), 'john@example.com');
    await user.click(screen.getByRole('button', { name: /send reset link/i }));

    await waitFor(() => {
      expect(screen.getByText('Check your email!')).toBeInTheDocument();
    });
    expect(screen.queryByLabelText(/^email/i)).not.toBeInTheDocument();
  });

  it('shows error toast on API error, form stays visible', async () => {
    const user = userEvent.setup();
    mockForgotPassword.mockReturnValue({
      unwrap: vi.fn().mockRejectedValue({
        data: { message: 'Network error' },
      }),
    });
    renderWithProviders(<ForgotPasswordPage />);

    await user.type(screen.getByLabelText(/^email/i), 'john@example.com');
    await user.click(screen.getByRole('button', { name: /send reset link/i }));

    await waitFor(() => {
      expect(screen.getByLabelText(/^email/i)).toBeInTheDocument();
    });
  });

  it('clears error when user types', async () => {
    const user = userEvent.setup();
    renderWithProviders(<ForgotPasswordPage />);

    await user.click(screen.getByRole('button', { name: /send reset link/i }));
    await waitFor(() => {
      expect(screen.getByText('Email is required')).toBeInTheDocument();
    });

    await user.type(screen.getByLabelText(/^email/i), 'a');
    expect(screen.queryByText('Email is required')).not.toBeInTheDocument();
  });
});
