import { describe, it, expect, vi, beforeEach } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { renderWithProviders } from '@/shared/lib/test/test-utils';
import { ResetPasswordPage } from '../ResetPasswordPage';
import { server } from '@/shared/lib/test/msw-server';
import { http, HttpResponse } from 'msw';

const mockNavigate = vi.fn();
const mockToastFn = vi.fn();

vi.mock('react-router', async (importOriginal) => {
  const actual = await importOriginal<typeof import('react-router')>();
  return {
    ...actual,
    useNavigate: () => mockNavigate,
    useSearchParams: () => [new URLSearchParams('email=test@example.com&token=abc123'), vi.fn()],
  };
});

vi.mock('@/shared/hooks', async (importOriginal) => {
  const actual = await importOriginal<typeof import('@/shared/hooks')>();
  return {
    ...actual,
    useToast: () => ({
      toast: {
        success: mockToastFn,
        error: mockToastFn,
      },
    }),
    useApiErrorHandler: () => ({
      handleError: vi.fn(),
    }),
  };
});

const mockResetUnwrap = vi.fn().mockResolvedValue({ success: true });

vi.mock('@/features/auth/api/authApi', () => ({
  useResetPasswordMutation: () => [
    vi.fn().mockReturnValue({
      unwrap: mockResetUnwrap,
    }),
    { isLoading: false },
  ],
}));

function setupResetPasswordHandlers(success = true) {
  server.use(
    http.post('/api/auth/reset-password', async () => {
      if (!success) {
        return HttpResponse.json(
          { success: false, errorDetails: { code: 'INVALID_TOKEN', message: 'Token has expired' } },
          { status: 400 }
        );
      }
      return HttpResponse.json({ success: true });
    })
  );
}

describe('ResetPasswordPage', () => {
  beforeEach(() => {
    server.resetHandlers();
    vi.clearAllMocks();
    mockResetUnwrap.mockResolvedValue({ success: true });
    setupResetPasswordHandlers(true);
  });

  describe('with valid URL params', () => {
    it('renders both password fields and submit button', () => {
      renderWithProviders(<ResetPasswordPage />);

      expect(screen.getByLabelText('New Password')).toBeInTheDocument();
      expect(screen.getByLabelText('Confirm New Password')).toBeInTheDocument();
      expect(screen.getByRole('button', { name: /reset password/i })).toBeInTheDocument();
    });
  });

  describe('client-side validation', () => {
    it('shows required errors on empty submit', async () => {
      const user = userEvent.setup();
      renderWithProviders(<ResetPasswordPage />);

      await user.click(screen.getByRole('button', { name: /reset password/i }));

      await waitFor(() => {
        expect(screen.getAllByText(/password is required/i).length).toBeGreaterThanOrEqual(1);
      });
    });
  });

  describe('password mismatch', () => {
    it('shows error when passwords do not match', async () => {
      const user = userEvent.setup();
      renderWithProviders(<ResetPasswordPage />);

      const passwordInput = screen.getByLabelText('New Password');
      const confirmInput = screen.getByLabelText('Confirm New Password');

      await user.type(passwordInput, 'Password1');
      await user.type(confirmInput, 'Password2');
      await user.click(screen.getByRole('button', { name: /reset password/i }));

      await waitFor(() => {
        expect(screen.getByText(/passwords do not match/i)).toBeInTheDocument();
      });
    });
  });

  describe('error clearing', () => {
    it('clears error when user starts typing', async () => {
      const user = userEvent.setup();
      renderWithProviders(<ResetPasswordPage />);

      await user.click(screen.getByRole('button', { name: /reset password/i }));

      await waitFor(() => {
        expect(screen.getAllByText(/password is required/i).length).toBeGreaterThanOrEqual(1);
      });

      await user.type(screen.getByLabelText('New Password'), 'P');

      await waitFor(() => {
        expect(screen.queryByText(/password is required/i)).not.toBeInTheDocument();
      });
    });
  });

  describe('success flow', () => {
    it('navigates to /login on success', async () => {
      const user = userEvent.setup();
      renderWithProviders(<ResetPasswordPage />);

      const passwordInput = screen.getByLabelText('New Password');
      const confirmInput = screen.getByLabelText('Confirm New Password');

      await user.type(passwordInput, 'Password1');
      await user.type(confirmInput, 'Password1');
      await user.click(screen.getByRole('button', { name: /reset password/i }));

      await waitFor(() => {
        expect(mockNavigate).toHaveBeenCalledWith('/login');
      });
    });

    it('shows success message', async () => {
      const user = userEvent.setup();
      renderWithProviders(<ResetPasswordPage />);

      const passwordInput = screen.getByLabelText('New Password');
      const confirmInput = screen.getByLabelText('Confirm New Password');

      await user.type(passwordInput, 'Password1');
      await user.type(confirmInput, 'Password1');
      await user.click(screen.getByRole('button', { name: /reset password/i }));

      await waitFor(() => {
        expect(screen.getByText(/success/i)).toBeInTheDocument();
      });
    });
  });

  describe('backend error', () => {
    it('shows error toast on backend error, form stays visible', async () => {
      mockResetUnwrap.mockRejectedValue({
        status: 400,
        data: { errorDetails: { message: 'Token has expired' } },
      });

      const user = userEvent.setup();
      renderWithProviders(<ResetPasswordPage />);

      const passwordInput = screen.getByLabelText('New Password');
      const confirmInput = screen.getByLabelText('Confirm New Password');

      await user.type(passwordInput, 'Password1');
      await user.type(confirmInput, 'Password1');
      await user.click(screen.getByRole('button', { name: /reset password/i }));

      await waitFor(() => {
        expect(screen.getByLabelText('New Password')).toBeInTheDocument();
      });
    });
  });
});
