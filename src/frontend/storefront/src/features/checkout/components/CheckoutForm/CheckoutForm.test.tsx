import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, it, expect, vi } from 'vitest';
import { renderWithProviders } from '@/shared/lib/test/test-utils';
import { CheckoutContext } from '../../context/CheckoutContext';
import type { UseCheckoutReturn } from '../../checkout.types';
import CheckoutForm from './CheckoutForm';

const mockFormData = {
  firstName: 'John',
  lastName: 'Doe',
  email: 'john@example.com',
  phone: '+1234567890',
  streetLine1: '123 Main St',
  city: 'New York',
  state: 'NY',
  postalCode: '10001',
  country: 'US',
};

function buildContext(overrides: Partial<UseCheckoutReturn> = {}): UseCheckoutReturn {
  return {
    formData: mockFormData,
    setFormData: vi.fn(),
    errors: {},
    promoCode: '',
    setPromoCode: vi.fn(),
    promoCodeValidation: null,
    validatingPromoCode: false,
    handleApplyPromoCode: vi.fn(),
    handleRemovePromoCode: vi.fn(),
    orderComplete: false,
    orderNumber: '',
    error: null,
    isGuestOrder: false,
    cartItems: [],
    subtotal: 0,
    isLoading: false,
    discount: 0,
    shipping: 0,
    tax: 0,
    total: 0,
    paymentMethod: 'credit_card',
    setPaymentMethod: vi.fn(),
    handleSubmit: vi.fn(),
    ...overrides,
  };
}

function renderForm(overrides: Partial<UseCheckoutReturn> = {}) {
  return renderWithProviders(
    <CheckoutContext.Provider value={buildContext(overrides)}>
      <CheckoutForm />
    </CheckoutContext.Provider>
  );
}

describe('CheckoutForm', () => {
  it('renders all form fields', () => {
    renderForm();

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

  it('calls setFormData when a field changes', async () => {
    const setFormData = vi.fn();
    const user = userEvent.setup();
    renderForm({ setFormData });

    const firstNameInput = screen.getByLabelText(/first name/i);
    await user.clear(firstNameInput);
    await user.type(firstNameInput, 'Jane');

    expect(setFormData).toHaveBeenCalled();
  });

  it('displays validation errors', () => {
    renderForm({
      errors: {
        firstName: 'First name is required',
        email: 'Invalid email address',
      },
    });

    expect(screen.getByText('First name is required')).toBeInTheDocument();
    expect(screen.getByText('Invalid email address')).toBeInTheDocument();
  });

  it('calls handleSubmit when form is submitted', async () => {
    const handleSubmit = vi.fn((e) => e.preventDefault());
    const user = userEvent.setup();
    renderForm({ handleSubmit });

    const submitButton = screen.getByRole('button', { name: /place order/i });
    await user.click(submitButton);

    expect(handleSubmit).toHaveBeenCalled();
  });
});
