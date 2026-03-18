import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, it, expect, vi } from 'vitest';
import { renderWithProviders } from '@/shared/lib/test/test-utils';
import CheckoutForm from './CheckoutForm';

describe('CheckoutForm', () => {
  const defaultProps = {
    onSubmit: vi.fn(),
    payment: { method: 'credit_card', onChange: vi.fn() },
  };

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

  it('updates field values on user input', async () => {
    const user = userEvent.setup();
    renderWithProviders(<CheckoutForm {...defaultProps} />);

    const firstNameInput = screen.getByLabelText(/first name/i);
    await user.type(firstNameInput, 'Jane');

    expect(firstNameInput).toHaveValue('Jane');
  });

  it('does not call onSubmit when required fields are empty', async () => {
    const onSubmit = vi.fn();
    const user = userEvent.setup();
    renderWithProviders(<CheckoutForm {...defaultProps} onSubmit={onSubmit} />);

    const submitButton = screen.getByRole('button', { name: /place order/i });
    await user.click(submitButton);

    expect(onSubmit).not.toHaveBeenCalled();
  });
});
