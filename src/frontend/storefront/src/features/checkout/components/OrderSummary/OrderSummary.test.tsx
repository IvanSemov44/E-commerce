import { fireEvent, render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, it, expect, vi } from 'vitest';
import { CheckoutContext } from '../../context/CheckoutContext';
import type { UseCheckoutReturn } from '../../checkout.types';
import { OrderSummary } from './OrderSummary';

const mockCartItems = [
  {
    id: '1',
    name: 'Product 1',
    slug: 'product-1',
    price: 29.99,
    quantity: 2,
    maxStock: 10,
    image: 'image1.jpg',
  },
  {
    id: '2',
    name: 'Product 2',
    slug: 'product-2',
    price: 49.99,
    quantity: 1,
    maxStock: 5,
    image: 'image2.jpg',
  },
];

function buildContext(overrides: Partial<UseCheckoutReturn> = {}): UseCheckoutReturn {
  return {
    formData: {
      firstName: '',
      lastName: '',
      email: '',
      phone: '',
      streetLine1: '',
      city: '',
      state: '',
      postalCode: '',
      country: '',
    },
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
    cartItems: mockCartItems,
    subtotal: 109.97,
    isLoading: false,
    discount: 0,
    shipping: 10,
    tax: 8.8,
    total: 128.77,
    paymentMethod: 'credit_card',
    setPaymentMethod: vi.fn(),
    handleSubmit: vi.fn(),
    ...overrides,
  };
}

function renderSummary(overrides: Partial<UseCheckoutReturn> = {}) {
  return render(
    <CheckoutContext.Provider value={buildContext(overrides)}>
      <OrderSummary />
    </CheckoutContext.Provider>
  );
}

describe('OrderSummary', () => {
  it('renders order summary heading', () => {
    renderSummary();
    expect(screen.getByRole('heading', { level: 2 })).toBeInTheDocument();
  });

  it('displays cart items with names and prices', () => {
    renderSummary();
    expect(screen.getByText('Product 1')).toBeInTheDocument();
    expect(screen.getByText('Product 2')).toBeInTheDocument();
  });

  it('displays totals breakdown', () => {
    renderSummary();
    expect(screen.getAllByText(/subtotal/i).length).toBeGreaterThan(0);
    expect(screen.getAllByText(/shipping/i).length).toBeGreaterThan(0);
    expect(screen.getAllByText(/tax/i).length).toBeGreaterThan(0);
    expect(screen.getAllByText(/total/i).length).toBeGreaterThan(0);
  });

  it('shows discount when discount > 0', () => {
    renderSummary({
      discount: 20,
      total: 108.77,
      promoCode: 'SAVE20',
      promoCodeValidation: { isValid: true, discountAmount: 20 },
    });
    expect(screen.getByText(/discount/i)).toBeInTheDocument();
  });

  it('calls setPromoCode when promo code input changes', () => {
    const setPromoCode = vi.fn();
    renderSummary({ setPromoCode });

    const promoInput = screen.getByLabelText(/promoCode|promo code|checkout\.promoCode/i);
    fireEvent.change(promoInput, { target: { value: 'SAVE10' } });

    expect(setPromoCode).toHaveBeenCalledWith('SAVE10');
  });

  it('calls handleRemovePromoCode when remove button is clicked', async () => {
    const handleRemovePromoCode = vi.fn();
    const user = userEvent.setup();
    renderSummary({ promoCode: 'SAVE10', handleRemovePromoCode });

    const removeButton = screen.getByRole('button', { name: /remove/i });
    await user.click(removeButton);

    expect(handleRemovePromoCode).toHaveBeenCalled();
  });
});
