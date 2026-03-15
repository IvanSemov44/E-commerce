import { fireEvent, render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, it, expect, vi } from 'vitest';
import { OrderSummary } from './OrderSummary';
import type { OrderSummaryProps } from './OrderSummary.types';

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

const defaultProps: OrderSummaryProps = {
  cartItems: mockCartItems,
  totals: {
    subtotal: 109.97,
    discount: 0,
    shipping: 10,
    tax: 8.8,
    total: 128.77,
  },
  promoCode: {
    code: '',
    validation: null,
    isValidating: false,
    onChange: vi.fn(),
    onApply: vi.fn(),
    onRemove: vi.fn(),
  },
};

describe('OrderSummary', () => {
  it('renders order summary heading', () => {
    render(<OrderSummary {...defaultProps} />);

    expect(screen.getByRole('heading', { level: 2 })).toBeInTheDocument();
  });

  it('displays cart items with images and prices', () => {
    render(<OrderSummary {...defaultProps} />);

    expect(screen.getByText('Product 1')).toBeInTheDocument();
    expect(screen.getByText('Product 2')).toBeInTheDocument();
  });

  it('displays totals breakdown', () => {
    render(<OrderSummary {...defaultProps} />);

    expect(screen.getAllByText(/subtotal/i).length).toBeGreaterThan(0);
    expect(screen.getAllByText(/shipping/i).length).toBeGreaterThan(0);
    expect(screen.getAllByText(/tax/i).length).toBeGreaterThan(0);
    expect(screen.getAllByText(/total/i).length).toBeGreaterThan(0);
  });

  it('shows discount when discount > 0', () => {
    render(
      <OrderSummary
        {...defaultProps}
        totals={{ ...defaultProps.totals, discount: 20, total: 108.77 }}
        promoCode={{
          ...defaultProps.promoCode,
          code: 'SAVE20',
          validation: { isValid: true, discountAmount: 20 },
        }}
      />
    );

    expect(screen.getByText(/discount/i)).toBeInTheDocument();
  });

  it('calls onChange when promo code input changes', () => {
    const onChange = vi.fn();
    render(<OrderSummary {...defaultProps} promoCode={{ ...defaultProps.promoCode, onChange }} />);

    const promoInput = screen.getByLabelText(/promoCode|promo code|checkout\.promoCode/i);
    fireEvent.change(promoInput, { target: { value: 'SAVE10' } });

    expect(onChange).toHaveBeenCalledWith('SAVE10');
  });

  it('shows remove button when promo code is already set', async () => {
    const user = userEvent.setup();
    const onRemove = vi.fn();
    render(
      <OrderSummary
        {...defaultProps}
        promoCode={{ ...defaultProps.promoCode, code: 'SAVE10', onRemove }}
      />
    );

    const removeButton = screen.getByRole('button', { name: /remove/i });
    await user.click(removeButton);

    expect(onRemove).toHaveBeenCalled();
  });
});
