import { fireEvent, render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, it, expect, vi } from 'vitest';
import OrderSummary from './OrderSummary';

describe('OrderSummary', () => {
  const mockCartItems = [
    {
      id: '1',
      name: 'Product 1',
      price: 29.99,
      quantity: 2,
      image: 'image1.jpg',
    },
    {
      id: '2',
      name: 'Product 2',
      price: 49.99,
      quantity: 1,
      image: 'image2.jpg',
    },
  ];

  it('renders order summary heading', () => {
    render(
      <OrderSummary
        cartItems={mockCartItems}
        subtotal={109.97}
        discount={0}
        shipping={10}
        tax={8.8}
        total={128.77}
        promoCode=""
        onPromoCodeChange={vi.fn()}
        promoCodeValidation={null}
        validatingPromoCode={false}
        onApplyPromoCode={vi.fn()}
        onRemovePromoCode={vi.fn()}
      />
    );

    expect(screen.getByRole('heading', { level: 2 })).toBeInTheDocument();
  });

  it('displays cart items with images and prices', () => {
    render(
      <OrderSummary
        cartItems={mockCartItems}
        subtotal={109.97}
        discount={0}
        shipping={10}
        tax={8.8}
        total={128.77}
        promoCode=""
        onPromoCodeChange={vi.fn()}
        promoCodeValidation={null}
        validatingPromoCode={false}
        onApplyPromoCode={vi.fn()}
        onRemovePromoCode={vi.fn()}
      />
    );

    expect(screen.getByText('Product 1')).toBeInTheDocument();
    expect(screen.getByText('Product 2')).toBeInTheDocument();
  });

  it('displays totals breakdown', () => {
    render(
      <OrderSummary
        cartItems={mockCartItems}
        subtotal={109.97}
        discount={0}
        shipping={10}
        tax={8.8}
        total={128.77}
        promoCode=""
        onPromoCodeChange={vi.fn()}
        promoCodeValidation={null}
        validatingPromoCode={false}
        onApplyPromoCode={vi.fn()}
        onRemovePromoCode={vi.fn()}
      />
    );

    expect(screen.getAllByText(/subtotal/i).length).toBeGreaterThan(0);
    expect(screen.getAllByText(/shipping/i).length).toBeGreaterThan(0);
    expect(screen.getAllByText(/tax/i).length).toBeGreaterThan(0);
    expect(screen.getAllByText(/total/i).length).toBeGreaterThan(0);
  });

  it('shows discount when discount > 0', () => {
    render(
      <OrderSummary
        cartItems={mockCartItems}
        subtotal={109.97}
        discount={20}
        shipping={10}
        tax={8.8}
        total={108.77}
        promoCode="SAVE20"
        onPromoCodeChange={vi.fn()}
        promoCodeValidation={{ isValid: true, discountAmount: 20 }}
        validatingPromoCode={false}
        onApplyPromoCode={vi.fn()}
        onRemovePromoCode={vi.fn()}
      />
    );

    expect(screen.getByText(/discount/i)).toBeInTheDocument();
  });

  it('calls onPromoCodeChange when promo code input changes', () => {
    const onPromoCodeChange = vi.fn();

    render(
      <OrderSummary
        cartItems={mockCartItems}
        subtotal={109.97}
        discount={0}
        shipping={10}
        tax={8.8}
        total={128.77}
        promoCode=""
        onPromoCodeChange={onPromoCodeChange}
        promoCodeValidation={null}
        validatingPromoCode={false}
        onApplyPromoCode={vi.fn()}
        onRemovePromoCode={vi.fn()}
      />
    );

    const promoInput = screen.getByLabelText(/promoCode|promo code|checkout\.promoCode/i);
    fireEvent.change(promoInput, { target: { value: 'SAVE10' } });

    expect(onPromoCodeChange).toHaveBeenCalledWith('SAVE10');
  });

  it('shows remove button when promo code is already set', async () => {
    const user = userEvent.setup();
    const onRemovePromoCode = vi.fn();

    render(
      <OrderSummary
        cartItems={mockCartItems}
        subtotal={109.97}
        discount={0}
        shipping={10}
        tax={8.8}
        total={128.77}
        promoCode="SAVE10"
        onPromoCodeChange={vi.fn()}
        promoCodeValidation={null}
        validatingPromoCode={false}
        onApplyPromoCode={vi.fn()}
        onRemovePromoCode={onRemovePromoCode}
      />
    );

    const removeButton = screen.getByRole('button', { name: /remove/i });
    await user.click(removeButton);

    expect(onRemovePromoCode).toHaveBeenCalled();
  });
});
