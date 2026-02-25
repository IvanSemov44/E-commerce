import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import CheckoutForm from '../../pages/components/Checkout/CheckoutForm';

describe('CheckoutForm', () => {
  const mockFormData = {
    firstName: '',
    lastName: '',
    email: '',
    phone: '',
    streetLine1: '',
    city: '',
    state: '',
    postalCode: '',
    country: '',
  };

  const mockHandlers = {
    onFormDataChange: vi.fn(),
    onSubmit: vi.fn((e) => e.preventDefault()),
  };

  it('renders all input fields', () => {
    render(<CheckoutForm formData={mockFormData} {...mockHandlers} />);

    // Use placeholder text since labels don't have for attributes
    expect(screen.getByPlaceholderText('John')).toBeInTheDocument();
    expect(screen.getByPlaceholderText('Doe')).toBeInTheDocument();
    expect(screen.getByPlaceholderText('your@email.com')).toBeInTheDocument();
    expect(screen.getByPlaceholderText('+1 (555) 123-4567')).toBeInTheDocument();
    expect(screen.getByPlaceholderText('123 Main St')).toBeInTheDocument();
    expect(screen.getByPlaceholderText('New York')).toBeInTheDocument();
    expect(screen.getByPlaceholderText('NY')).toBeInTheDocument();
    expect(screen.getByPlaceholderText('10001')).toBeInTheDocument();
    expect(screen.getByPlaceholderText('United States')).toBeInTheDocument();
  });

  it('calls onFormDataChange when input values change', async () => {
    const user = userEvent.setup();
    render(<CheckoutForm formData={mockFormData} {...mockHandlers} />);

    const firstNameInput = screen.getByPlaceholderText('John');
    await user.type(firstNameInput, 'John');

    expect(mockHandlers.onFormDataChange).toHaveBeenCalled();
    // Verify the last call arguments
    expect(mockHandlers.onFormDataChange).toHaveBeenLastCalledWith(
      expect.objectContaining({ firstName: 'n' }) // userEvent types char by char
    );
  });

  it('displays validation errors when provided', () => {
    const errors = {
      email: 'Invalid email address',
      postalCode: 'Zip code is required',
    };

    render(
      <CheckoutForm
        formData={mockFormData}
        {...mockHandlers}
        errors={errors}
      />
    );

    expect(screen.getByText('Invalid email address')).toBeInTheDocument();
    expect(screen.getByText('Zip code is required')).toBeInTheDocument();
  });

  it('calls onSubmit when form is submitted', async () => {
    const user = userEvent.setup();
    // Fill in required fields to make form valid
    const filledData = {
      ...mockFormData,
      firstName: 'John',
      lastName: 'Doe',
      email: 'john@example.com',
      phone: '+1234567890',
      streetLine1: '123 Main St',
      city: 'New York',
      state: 'NY',
      postalCode: '10001',
      country: 'USA',
    };

    render(<CheckoutForm formData={filledData} {...mockHandlers} />);

    const submitButton = screen.getByRole('button', { name: /place order/i });
    await user.click(submitButton);

    expect(mockHandlers.onSubmit).toHaveBeenCalled();
  });

  it('pre-fills inputs with formData values', () => {
    const filledData = {
      ...mockFormData,
      firstName: 'Jane',
      email: 'jane@example.com',
    };

    render(<CheckoutForm formData={filledData} {...mockHandlers} />);

    expect(screen.getByPlaceholderText('John')).toHaveValue('Jane');
    expect(screen.getByPlaceholderText('your@email.com')).toHaveValue('jane@example.com');
  });
});
