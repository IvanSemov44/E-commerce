import { describe, it, expect, vi, beforeEach } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { renderWithProviders } from '@/shared/lib/test/test-utils';
import { LoginPage } from '../LoginPage';
import { server } from '@/shared/lib/test/msw-server';
import { http, HttpResponse } from 'msw';

const mockNavigate = vi.fn();

vi.mock('react-router', async (importOriginal) => {
  const actual = await importOriginal<typeof import('react-router')>();
  return { ...actual, useNavigate: () => mockNavigate };
});

function setupLoginHandlers(success = true) {
  server.use(
    http.post('/api/auth/login', async ({ request }) => {
      if (!success) {
        return HttpResponse.json(
          {
            success: false,
            errorDetails: { message: 'Invalid credentials', code: 'INVALID_CREDENTIALS' },
          },
          { status: 401 }
        );
      }
      const body = await request.json();
      return HttpResponse.json({
        success: true,
        data: {
          user: {
            id: '1',
            email: body.email,
            firstName: 'John',
            lastName: 'Doe',
            role: 'Customer',
          },
        },
      });
    })
  );
}

async function fillValidForm(user: ReturnType<typeof userEvent.setup>) {
  await user.type(screen.getByLabelText(/^email/i), 'john@example.com');
  await user.type(screen.getByLabelText(/^password$/i), 'password123');
}

describe('LoginPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    setupLoginHandlers(true);
  });

  it('renders email field, password field, submit button, forgot password link', () => {
    renderWithProviders(<LoginPage />);

    expect(screen.getByLabelText(/^email/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/^password$/i)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /^login$/i })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: /forgot password/i })).toBeInTheDocument();
  });

  it('shows client-side required errors on empty submit — mutation not called', async () => {
    const user = userEvent.setup();
    renderWithProviders(<LoginPage />);

    await user.click(screen.getByRole('button', { name: /^login$/i }));

    await waitFor(() => {
      expect(screen.getByText('Email is required')).toBeInTheDocument();
    });
    expect(screen.getByText('Password is required')).toBeInTheDocument();
  });

  it('dispatches loginSuccess and navigates home on success', async () => {
    const user = userEvent.setup();
    const { store } = renderWithProviders(<LoginPage />);

    await fillValidForm(user);
    await user.click(screen.getByRole('button', { name: /^login$/i }));

    await waitFor(() => {
      expect(mockNavigate).toHaveBeenCalledWith('/');
    });
    expect(store.getState().auth.isAuthenticated).toBe(true);
  });

  it('shows backend error via handleError, no navigation', async () => {
    const user = userEvent.setup();
    setupLoginHandlers(false);
    renderWithProviders(<LoginPage />);

    await fillValidForm(user);
    await user.click(screen.getByRole('button', { name: /^login$/i }));

    await waitFor(() => {
      expect(mockNavigate).not.toHaveBeenCalled();
    });
  });

  it('clears a field error as soon as the user types', async () => {
    const user = userEvent.setup();
    renderWithProviders(<LoginPage />);

    await user.click(screen.getByRole('button', { name: /^login$/i }));
    await waitFor(() => {
      expect(screen.getByText('Email is required')).toBeInTheDocument();
    });

    await user.type(screen.getByLabelText(/^email/i), 'a');
    expect(screen.queryByText('Email is required')).not.toBeInTheDocument();
  });

  it('password visibility toggle changes input type between password and text', async () => {
    const user = userEvent.setup();
    renderWithProviders(<LoginPage />);

    const passwordInput = screen.getByLabelText(/^password$/i);
    expect(passwordInput).toHaveAttribute('type', 'password');

    const toggleButton = screen.getByRole('button', { name: /show password/i });
    await user.click(toggleButton);

    expect(passwordInput).toHaveAttribute('type', 'text');

    await user.click(toggleButton);
    expect(passwordInput).toHaveAttribute('type', 'password');
  });
});
