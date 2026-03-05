import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, it, expect, vi } from 'vitest';
import CheckoutForm from './CheckoutForm';

describe('CheckoutForm', () => {
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

  it('renders all form fields', () => {
    const onFormDataChange = vi.fn();
    const onSubmit = vi.fn();

    render(
      <CheckoutForm
        formData={mockFormData}
        errors={{}}
        onFormDataChange={onFormDataChange}
        onSubmit={onSubmit}
        isAuthenticated={false}
      />
    );

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

  it('updates form data on field change', async () => {
    const user = userEvent.setup();
    const onFormDataChange = vi.fn();
    const onSubmit = vi.fn();

    render(
      <CheckoutForm
        formData={mockFormData}
        errors={{}}
        onFormDataChange={onFormDataChange}
        onSubmit={onSubmit}
        isAuthenticated={false}
      />
    );

    const firstNameInput = screen.getByLabelText(/first name/i);
    await user.clear(firstNameInput);
    await user.type(firstNameInput, 'Jane');

    expect(onFormDataChange).toHaveBeenCalled();
  });

  it('displays validation errors', () => {
    const errors = {
      firstName: 'First name is required',
      email: 'Invalid email address',
    };
    const onFormDataChange = vi.fn();
    const onSubmit = vi.fn();

    render(
      <CheckoutForm
        formData={mockFormData}
        errors={errors}
        onFormDataChange={onFormDataChange}
        onSubmit={onSubmit}
        isAuthenticated={false}
      />
    );

    expect(screen.getByText('First name is required')).toBeInTheDocument();
    expect(screen.getByText('Invalid email address')).toBeInTheDocument();
  });

  it('submits form on button click', async () => {
    const user = userEvent.setup();
    const onFormDataChange = vi.fn();
    const onSubmit = vi.fn();

    render(
      <CheckoutForm
        formData={mockFormData}
        errors={{}}
        onFormDataChange={onFormDataChange}
        onSubmit={onSubmit}
        isAuthenticated={false}
      />
    );

    const submitButton = screen.getByRole('button', { name: /place order/i });
    await user.click(submitButton);

    expect(onSubmit).toHaveBeenCalled();
  });
});
