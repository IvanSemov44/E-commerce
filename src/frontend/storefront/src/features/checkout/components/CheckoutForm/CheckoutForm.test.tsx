import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { renderWithProviders } from '@/shared/lib/test/test-utils';
import { CheckoutForm } from './CheckoutForm';

const defaultProps = {
  onSubmit: vi.fn(),
  payment: { method: 'credit_card', onChange: vi.fn() },
};

const validFormData = {
  firstName: 'Jane',
  lastName: 'Doe',
  email: 'jane@example.com',
  phone: '+12345678901',
  address: '123 Main St',
  city: 'New York',
  state: 'NY',
  postalCode: '10001',
};

async function fillForm(user: ReturnType<typeof userEvent.setup>) {
  await user.type(screen.getByLabelText(/first name/i), validFormData.firstName);
  await user.type(screen.getByLabelText(/last name/i), validFormData.lastName);
  await user.type(screen.getByLabelText(/email/i), validFormData.email);
  await user.type(screen.getByLabelText(/phone/i), validFormData.phone);
  await user.type(screen.getByLabelText(/address/i), validFormData.address);
  await user.type(screen.getByLabelText(/city/i), validFormData.city);
  await user.type(screen.getByLabelText(/state/i), validFormData.state);
  await user.type(screen.getByLabelText(/postal code/i), validFormData.postalCode);
  await user.selectOptions(screen.getByLabelText(/country/i), 'US');
}

describe('CheckoutForm', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  // ── Rendering ─────────────────────────────────────────────────────────────

  it('renders all form fields', () => {
    renderWithProviders(<CheckoutForm {...defaultProps} />);

    expect(screen.getByLabelText(/first name/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/last name/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/email/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/phone/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/address/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/city/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/state/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/postal code/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/country/i)).toBeInTheDocument();
  });

  it('renders submit button', () => {
    renderWithProviders(<CheckoutForm {...defaultProps} />);

    expect(screen.getByRole('button', { name: /place order/i })).toBeInTheDocument();
  });

  // ── Field interaction ──────────────────────────────────────────────────────

  it('updates field value on user input', async () => {
    const user = userEvent.setup();
    renderWithProviders(<CheckoutForm {...defaultProps} />);

    await user.type(screen.getByLabelText(/first name/i), 'Jane');

    expect(screen.getByLabelText(/first name/i)).toHaveValue('Jane');
  });

  it('updates country select value', async () => {
    const user = userEvent.setup();
    renderWithProviders(<CheckoutForm {...defaultProps} />);

    await user.selectOptions(screen.getByLabelText(/country/i), 'US');

    expect(screen.getByLabelText(/country/i)).toHaveValue('US');
  });

  // ── Validation ────────────────────────────────────────────────────────────

  it('shows required error for each empty field on submit', async () => {
    const user = userEvent.setup();
    renderWithProviders(<CheckoutForm {...defaultProps} />);

    await user.click(screen.getByRole('button', { name: /place order/i }));

    await waitFor(() => {
      expect(screen.getByText('First name is required')).toBeInTheDocument();
      expect(screen.getByText('Last name is required')).toBeInTheDocument();
      expect(screen.getByText('Email is required')).toBeInTheDocument();
      expect(screen.getByText('Phone is required')).toBeInTheDocument();
      expect(screen.getByText('Street address is required')).toBeInTheDocument();
      expect(screen.getByText('City is required')).toBeInTheDocument();
      expect(screen.getByText('State is required')).toBeInTheDocument();
      expect(screen.getByText('Postal code is required')).toBeInTheDocument();
      expect(screen.getByText('Country is required')).toBeInTheDocument();
    });
  });

  it('shows invalid email error for malformed email', async () => {
    const user = userEvent.setup();
    renderWithProviders(<CheckoutForm {...defaultProps} />);

    await user.type(screen.getByLabelText(/email/i), 'not-an-email');
    await user.click(screen.getByRole('button', { name: /place order/i }));

    await waitFor(() => {
      expect(screen.getByText('Invalid email address')).toBeInTheDocument();
    });
  });

  it('shows invalid phone error for short phone number', async () => {
    const user = userEvent.setup();
    renderWithProviders(<CheckoutForm {...defaultProps} />);

    await user.type(screen.getByLabelText(/phone/i), '123');
    await user.click(screen.getByRole('button', { name: /place order/i }));

    await waitFor(() => {
      expect(screen.getByText('Invalid phone number')).toBeInTheDocument();
    });
  });

  it('clears field error when user starts typing', async () => {
    const user = userEvent.setup();
    renderWithProviders(<CheckoutForm {...defaultProps} />);

    await user.click(screen.getByRole('button', { name: /place order/i }));
    await waitFor(() => {
      expect(screen.getByText('First name is required')).toBeInTheDocument();
    });

    await user.type(screen.getByLabelText(/first name/i), 'J');

    await waitFor(() => {
      expect(screen.queryByText('First name is required')).not.toBeInTheDocument();
    });
  });

  it('does not call onSubmit when form is invalid', async () => {
    const onSubmit = vi.fn();
    const user = userEvent.setup();
    renderWithProviders(<CheckoutForm {...defaultProps} onSubmit={onSubmit} />);

    await user.click(screen.getByRole('button', { name: /place order/i }));

    expect(onSubmit).not.toHaveBeenCalled();
  });

  // ── Submission ────────────────────────────────────────────────────────────

  it('calls onSubmit with correct values when form is valid', async () => {
    const onSubmit = vi.fn();
    const user = userEvent.setup();
    renderWithProviders(<CheckoutForm {...defaultProps} onSubmit={onSubmit} />);

    await fillForm(user);
    await user.click(screen.getByRole('button', { name: /place order/i }));

    await waitFor(() => {
      expect(onSubmit).toHaveBeenCalledWith(
        expect.objectContaining({
          firstName: 'Jane',
          lastName: 'Doe',
          email: 'jane@example.com',
          phone: '+12345678901',
          city: 'New York',
          state: 'NY',
          postalCode: '10001',
          country: 'US',
        })
      );
    });
  });

  // ── Submit button state ───────────────────────────────────────────────────

  it('disables submit button while submitting', async () => {
    // onSubmit returns a promise that never resolves — simulates in-flight request
    const onSubmit = vi.fn(() => new Promise<void>(() => {}));
    const user = userEvent.setup();
    renderWithProviders(<CheckoutForm {...defaultProps} onSubmit={onSubmit} />);

    await fillForm(user);
    await user.click(screen.getByRole('button', { name: /place order/i }));

    await waitFor(() => {
      expect(screen.getByRole('button', { name: /loading/i })).toBeDisabled();
    });
  });

  it('re-enables submit button after submission completes', async () => {
    const onSubmit = vi.fn().mockResolvedValue(undefined);
    const user = userEvent.setup();
    renderWithProviders(<CheckoutForm {...defaultProps} onSubmit={onSubmit} />);

    await fillForm(user);
    await user.click(screen.getByRole('button', { name: /place order/i }));

    await waitFor(() => {
      expect(screen.getByRole('button', { name: /place order/i })).not.toBeDisabled();
    });
  });
});
