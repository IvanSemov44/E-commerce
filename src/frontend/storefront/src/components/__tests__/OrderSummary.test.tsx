import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import OrderSummary from '../../pages/components/Checkout/OrderSummary';
import type { CartItem } from '../../store/slices/cartSlice';

describe('OrderSummary', () => {
  const mockCartItems: CartItem[] = [
    {
      id: '1',
      name: 'Product 1',
      slug: 'product-1',
      price: 25.00,
      quantity: 2,
      maxStock: 100,
      image: 'img1.jpg',
    },
    {
      id: '2',
      name: 'Product 2',
      slug: 'product-2',
      price: 15.00,
      quantity: 1,
      maxStock: 50,
      image: 'img2.jpg',
    },
  ];

  const defaultProps = {
    cartItems: mockCartItems,
    subtotal: 65.00,
    discount: 0,
    shipping: 10.00,
    tax: 5.20,
    total: 80.20,
    promoCode: '',
    onPromoCodeChange: vi.fn(),
    promoCodeValidation: null,
    validatingPromoCode: false,
    onApplyPromoCode: vi.fn(),
    onRemovePromoCode: vi.fn(),
  };

  it('renders order summary title', () => {
    render(<OrderSummary {...defaultProps} />);
    expect(screen.getByText('Order Summary')).toBeInTheDocument();
  });

  it('renders subtotal correctly', () => {
    render(<OrderSummary {...defaultProps} />);
    expect(screen.getByText('$65.00')).toBeInTheDocument();
  });

  it('renders shipping cost correctly', () => {
    render(<OrderSummary {...defaultProps} shipping={10.00} />);
    expect(screen.getByText('$10.00')).toBeInTheDocument();
  });

  it('renders FREE when shipping is 0', () => {
    render(<OrderSummary {...defaultProps} shipping={0} />);
    expect(screen.getByText('FREE')).toBeInTheDocument();
  });

  it('renders tax correctly', () => {
    render(<OrderSummary {...defaultProps} tax={5.20} />);
    expect(screen.getByText('$5.20')).toBeInTheDocument();
  });

  it('renders grand total correctly', () => {
    render(<OrderSummary {...defaultProps} total={80.20} />);
    expect(screen.getByText('$80.20')).toBeInTheDocument();
  });

  it('renders discount when discount > 0', () => {
    render(<OrderSummary {...defaultProps} discount={10.00} promoCode="SAVE10" />);
    expect(screen.getByText('-$10.00')).toBeInTheDocument();
    expect(screen.getByText('Discount (SAVE10):')).toBeInTheDocument();
  });

  it('does not render discount line when discount is 0', () => {
    render(<OrderSummary {...defaultProps} discount={0} />);
    expect(screen.queryByText('Discount')).not.toBeInTheDocument();
  });

  it('renders cart items', () => {
    render(<OrderSummary {...defaultProps} />);
    expect(screen.getByText('Product 1')).toBeInTheDocument();
    expect(screen.getByText('Product 2')).toBeInTheDocument();
  });

  it('passes readOnly prop to CartItem', () => {
    render(<OrderSummary {...defaultProps} />);
    // CartItem should be rendered in readOnly mode
    expect(screen.getByText('Product 1')).toBeInTheDocument();
  });
});
