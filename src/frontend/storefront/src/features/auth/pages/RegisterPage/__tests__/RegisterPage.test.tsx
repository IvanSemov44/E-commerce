import { describe, it, expect, vi, beforeEach } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { renderWithProviders } from '@/shared/lib/test/test-utils';
import { RegisterPage } from '../RegisterPage';
import { server } from '@/shared/lib/test/msw-server';
import { http, HttpResponse } from 'msw';

const mockNavigate = vi.fn();

vi.mock('react-router', async (importOriginal) => {
  const actual = await importOriginal<typeof import('react-router')>();
  return { ...actual, useNavigate: () => mockNavigate };
});

function setupRegisterHandlers(mode: 'success' | 'duplicate' | 'validation' = 'success') {
  server.use(
    http.post('/api/auth/register', async ({ request }) => {
      const body = await request.json();

      if (mode === 'duplicate') {
        return HttpResponse.json(
          {
            success: false,
            errorDetails: { code: 'DUPLICATE_EMAIL', message: 'Email is already in use' },
          },
          { status: 409 }
        );
      }

      if (mode === 'validation') {
        return HttpResponse.json(
          { success: false, errors: { Password: ['Password must contain a special character'] } },
          { status: 400 }
        );
      }

      return HttpResponse.json({
        success: true,
        data: {
          user: {
            id: '1',
            email: body.email,
            firstName: body.firstName,
            lastName: body.lastName,
            role: 'Customer',
          },
        },
      });
    })
  );
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
    setupRegisterHandlers('success');
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
  });

  it('shows terms error when terms not accepted', async () => {
    const user = userEvent.setup();
    renderWithProviders(<RegisterPage />);

    await fillValidForm(user);
    await user.click(screen.getByRole('button', { name: /^register$/i }));

    await waitFor(() => {
      expect(screen.getByText('You must accept the terms to continue')).toBeInTheDocument();
    });
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
  });

  it('shows DUPLICATE_EMAIL backend error inline on the email field', async () => {
    const user = userEvent.setup();
    setupRegisterHandlers('duplicate');
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
    setupRegisterHandlers('validation');
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
