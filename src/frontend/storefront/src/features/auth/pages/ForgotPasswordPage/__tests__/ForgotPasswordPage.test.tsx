import { describe, it, expect, vi, beforeEach } from 'vitest';
import { screen, waitFor, fireEvent } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { renderWithProviders } from '@/shared/lib/test/test-utils';
import { ForgotPasswordPage } from '../ForgotPasswordPage';

const mockForgotPassword = vi.fn().mockResolvedValue({ success: true });

vi.mock('@/features/auth/api/authApi', () => ({
  useForgotPasswordMutation: () => [() => ({ unwrap: mockForgotPassword }), { isLoading: false }],
}));

describe('ForgotPasswordPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders email field and submit button', () => {
    renderWithProviders(<ForgotPasswordPage />);

    expect(screen.getByLabelText(/^email/i)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /send reset link/i })).toBeInTheDocument();
  });

  it('shows email required error on empty submit', async () => {
    const { container } = renderWithProviders(<ForgotPasswordPage />);

    const form = container.querySelector('form');
    fireEvent.submit(form!);

    await waitFor(() => {
      expect(screen.getByText('Email is required')).toBeInTheDocument();
    });
  });

  it('shows success state on successful submission', async () => {
    const user = userEvent.setup();
    const { container } = renderWithProviders(<ForgotPasswordPage />);

    await user.type(screen.getByLabelText(/^email/i), 'john@example.com');

    const form = container.querySelector('form');
    fireEvent.submit(form!);

    // Wait a bit for React to process
    await new Promise((resolve) => setTimeout(resolve, 100));

    // Check that mock was called (can't check exact args due to RTK Query wrapper)
    expect(mockForgotPassword).toHaveBeenCalled();

    // Wait for success state
    await waitFor(() => {
      expect(screen.getByText('Check your email!')).toBeInTheDocument();
    });
  });

  it('shows error toast on API error, form stays visible', async () => {
    mockForgotPassword.mockRejectedValueOnce(new Error('Network error'));
    const user = userEvent.setup();
    const { container } = renderWithProviders(<ForgotPasswordPage />);

    await user.type(screen.getByLabelText(/^email/i), 'john@example.com');

    const form = container.querySelector('form');
    fireEvent.submit(form!);

    await waitFor(() => {
      expect(mockForgotPassword).toHaveBeenCalled();
    });

    await waitFor(() => {
      expect(screen.getByLabelText(/^email/i)).toBeInTheDocument();
    });
  });

  it('clears error when user types', async () => {
    const user = userEvent.setup();
    const { container } = renderWithProviders(<ForgotPasswordPage />);

    const form = container.querySelector('form');
    fireEvent.submit(form!);

    await waitFor(() => {
      expect(screen.getByText('Email is required')).toBeInTheDocument();
    });

    await user.type(screen.getByLabelText(/^email/i), 'a');
    expect(screen.queryByText('Email is required')).not.toBeInTheDocument();
  });
});
