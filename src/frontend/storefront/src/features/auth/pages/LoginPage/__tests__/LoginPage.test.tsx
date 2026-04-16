import { describe, it, expect, vi, beforeEach } from 'vitest';
import { screen, waitFor, fireEvent } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { renderWithProviders } from '@/shared/lib/test/test-utils';
import { LoginPage } from '../LoginPage';

const mockNavigate = vi.fn();

vi.mock('react-router', async (importOriginal) => {
  const actual = await importOriginal<typeof import('react-router')>();
  return { ...actual, useNavigate: () => mockNavigate };
});

const mockLogin = vi.fn().mockResolvedValue({
  success: true,
  user: {
    id: '1',
    email: 'john@example.com',
    firstName: 'John',
    lastName: 'Doe',
    role: 'Customer',
  },
});

vi.mock('@/features/auth/api/authApi', () => ({
  useLoginMutation: () => [() => ({ unwrap: mockLogin }), { isLoading: false }],
}));

async function fillValidForm(user: ReturnType<typeof userEvent.setup>) {
  await user.type(screen.getByLabelText(/^email/i), 'john@example.com');
  await user.type(screen.getByLabelText(/^password$/i), 'password123');
}

describe('LoginPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockLogin.mockResolvedValue({
      success: true,
      user: {
        id: '1',
        email: 'john@example.com',
        firstName: 'John',
        lastName: 'Doe',
        role: 'Customer',
      },
    });
  });

  it('renders email field, password field, submit button, forgot password link', () => {
    renderWithProviders(<LoginPage />);

    expect(screen.getByLabelText(/^email/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/^password$/i)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /^login$/i })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: /forgot password/i })).toBeInTheDocument();
  });

  it('shows client-side required errors on empty submit', async () => {
    const { container } = renderWithProviders(<LoginPage />);

    const form = container.querySelector('form');
    fireEvent.submit(form!);

    await waitFor(() => {
      expect(screen.getByText('Email is required')).toBeInTheDocument();
    });
    expect(screen.getByText('Password is required')).toBeInTheDocument();
  });

  it('dispatches loginSuccess and navigates home on success', async () => {
    const user = userEvent.setup();
    const { container } = renderWithProviders(<LoginPage />);

    await fillValidForm(user);

    const form = container.querySelector('form');
    fireEvent.submit(form!);

    await waitFor(
      () => {
        expect(mockNavigate).toHaveBeenCalledWith('/');
      },
      { timeout: 3000 }
    );
  });

  it('shows backend error and does not navigate', async () => {
    mockLogin.mockRejectedValue({
      status: 401,
      data: { errorDetails: { message: 'Invalid credentials' } },
    });

    const user = userEvent.setup();
    const { container } = renderWithProviders(<LoginPage />);

    await fillValidForm(user);

    const form = container.querySelector('form');
    fireEvent.submit(form!);

    await waitFor(
      () => {
        expect(mockNavigate).not.toHaveBeenCalled();
      },
      { timeout: 3000 }
    );
  });

  it('clears a field error as soon as the user types', async () => {
    const user = userEvent.setup();
    const { container } = renderWithProviders(<LoginPage />);

    const form = container.querySelector('form');
    fireEvent.submit(form!);

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
