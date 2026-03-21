import { describe, it, expect, vi, beforeEach } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { renderWithProviders } from '@/shared/lib/test/test-utils';
import { RegisterPage } from '../RegisterPage';

const mockNavigate = vi.fn();
const mockRegister = vi.fn();

vi.mock('@/features/auth/api/authApi', () => ({
  useRegisterMutation: () => [mockRegister, {}],
}));

vi.mock('react-router', async (importOriginal) => {
  const actual = await importOriginal<typeof import('react-router')>();
  return { ...actual, useNavigate: () => mockNavigate };
});

function successResponse() {
  return {
    unwrap: vi.fn().mockResolvedValue({
      success: true,
      user: {
        id: '1',
        email: 'john@example.com',
        firstName: 'John',
        lastName: 'Doe',
        role: 'Customer',
      },
    }),
  };
}

async function fillValidForm(user: ReturnType<typeof userEvent.setup>) {
  await user.type(screen.getByLabelText(/^first name/i), 'John');
  await user.type(screen.getByLabelText(/^last name/i), 'Doe');
  await user.type(screen.getByLabelText(/^email/i), 'john@example.com');
  await user.type(screen.getByLabelText(/^password$/i), 'Password1');
  await user.type(screen.getByLabelText(/confirm password/i), 'Password1');
}

describe('RegisterPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockRegister.mockReturnValue(successResponse());
  });

  it('renders all form fields and submit button', () => {
    renderWithProviders(<RegisterPage />);

    expect(screen.getByLabelText(/^first name/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/^last name/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/^email/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/^password$/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/confirm password/i)).toBeInTheDocument();
    expect(screen.getByRole('checkbox')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /^register$/i })).toBeInTheDocument();
  });

  it('shows required-field errors on empty submit', async () => {
    const user = userEvent.setup();
    renderWithProviders(<RegisterPage />);

    await user.click(screen.getByRole('button', { name: /^register$/i }));

    await waitFor(() => {
      expect(screen.getByText('First Name required')).toBeInTheDocument();
    });
    expect(screen.getByText('Last Name required')).toBeInTheDocument();
    expect(screen.getByText('Email is required')).toBeInTheDocument();
    expect(screen.getByText('Password is required')).toBeInTheDocument();
    expect(screen.getByText('Confirm Password required')).toBeInTheDocument();
    expect(mockRegister).not.toHaveBeenCalled();
  });

  it('shows terms error when terms not accepted', async () => {
    const user = userEvent.setup();
    renderWithProviders(<RegisterPage />);

    await fillValidForm(user);
    await user.click(screen.getByRole('button', { name: /^register$/i }));

    await waitFor(() => {
      expect(screen.getByText('You must accept the terms to continue')).toBeInTheDocument();
    });
    expect(mockRegister).not.toHaveBeenCalled();
  });

  it('dispatches loginSuccess and navigates home on success', async () => {
    const user = userEvent.setup();
    const { store } = renderWithProviders(<RegisterPage />);

    await fillValidForm(user);
    await user.click(screen.getByRole('checkbox'));
    await user.click(screen.getByRole('button', { name: /^register$/i }));

    await waitFor(() => {
      expect(mockNavigate).toHaveBeenCalledWith('/');
    });
    expect(store.getState().auth.isAuthenticated).toBe(true);
    expect(mockRegister).toHaveBeenCalledWith({
      firstName: 'John',
      lastName: 'Doe',
      email: 'john@example.com',
      password: 'Password1',
    });
  });

  it('shows DUPLICATE_EMAIL backend error inline on the email field', async () => {
    const user = userEvent.setup();
    mockRegister.mockReturnValue({
      unwrap: vi.fn().mockRejectedValue({
        data: { errorDetails: { code: 'DUPLICATE_EMAIL', message: 'Email is already in use' } },
      }),
    });
    renderWithProviders(<RegisterPage />);

    await fillValidForm(user);
    await user.click(screen.getByRole('checkbox'));
    await user.click(screen.getByRole('button', { name: /^register$/i }));

    await waitFor(() => {
      expect(screen.getByText('Email is already in use')).toBeInTheDocument();
    });
    expect(screen.getByLabelText(/^email/i)).toHaveAttribute('aria-invalid', 'true');
    expect(mockNavigate).not.toHaveBeenCalled();
  });

  it('shows ASP.NET validation errors inline on the matching field', async () => {
    const user = userEvent.setup();
    mockRegister.mockReturnValue({
      unwrap: vi.fn().mockRejectedValue({
        data: { errors: { Password: ['Password must contain a special character'] } },
      }),
    });
    renderWithProviders(<RegisterPage />);

    await fillValidForm(user);
    await user.click(screen.getByRole('checkbox'));
    await user.click(screen.getByRole('button', { name: /^register$/i }));

    await waitFor(() => {
      expect(screen.getByText('Password must contain a special character')).toBeInTheDocument();
    });
    expect(mockNavigate).not.toHaveBeenCalled();
  });

  it('clears a field error as soon as the user types', async () => {
    const user = userEvent.setup();
    renderWithProviders(<RegisterPage />);

    await user.click(screen.getByRole('button', { name: /^register$/i }));
    await waitFor(() => {
      expect(screen.getByText('Email is required')).toBeInTheDocument();
    });

    await user.type(screen.getByLabelText(/^email/i), 'a');
    expect(screen.queryByText('Email is required')).not.toBeInTheDocument();
  });
});
