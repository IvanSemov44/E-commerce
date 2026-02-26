import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import PromoCodeSection from '../../pages/components/Checkout/PromoCodeSection';

describe('PromoCodeSection', () => {
  const defaultProps = {
    promoCode: '',
    onPromoCodeChange: vi.fn(),
    promoCodeValidation: null,
    validatingPromoCode: false,
    onApply: vi.fn(),
    onRemove: vi.fn(),
  };

  it('renders promo code input field', () => {
    render(<PromoCodeSection {...defaultProps} />);
    expect(screen.getByPlaceholderText('Enter promo code')).toBeInTheDocument();
  });

  it('renders Apply button', () => {
    render(<PromoCodeSection {...defaultProps} promoCode="SAVE10" />);
    expect(screen.getByText('Apply')).toBeInTheDocument();
  });

  it('disables Apply button when promo code is empty', () => {
    render(<PromoCodeSection {...defaultProps} promoCode="" />);
    const button = screen.getByText('Apply') as HTMLButtonElement;
    expect(button.disabled).toBe(true);
  });

  it('disables Apply button when validating', () => {
    render(<PromoCodeSection {...defaultProps} promoCode="SAVE10" validatingPromoCode={true} />);
    const button = screen.getByText('Validating...') as HTMLButtonElement;
    expect(button.disabled).toBe(true);
  });

  it('shows Validating text when validatingPromoCode is true', () => {
    render(<PromoCodeSection {...defaultProps} promoCode="SAVE10" validatingPromoCode={true} />);
    expect(screen.getByText('Validating...')).toBeInTheDocument();
  });

  it('calls onPromoCodeChange when input changes', () => {
    const onPromoCodeChange = vi.fn();
    render(<PromoCodeSection {...defaultProps} onPromoCodeChange={onPromoCodeChange} />);
    
    const input = screen.getByPlaceholderText('Enter promo code');
    fireEvent.change(input, { target: { value: 'test' } });
    
    expect(onPromoCodeChange).toHaveBeenCalledWith('TEST'); // Converted to uppercase
  });

  it('calls onApply when Apply button is clicked', () => {
    const onApply = vi.fn();
    render(<PromoCodeSection {...defaultProps} promoCode="SAVE10" onApply={onApply} />);
    
    fireEvent.click(screen.getByText('Apply'));
    expect(onApply).toHaveBeenCalledTimes(1);
  });

  it('displays success message when promo code is valid', () => {
    render(
      <PromoCodeSection
        {...defaultProps}
        promoCode="SAVE10"
        promoCodeValidation={{ isValid: true, discountAmount: 10.00, message: 'Promo code applied!' }}
      />
    );
    expect(screen.getByText('Promo code applied!')).toBeInTheDocument();
  });

  it('displays error message when promo code is invalid', () => {
    render(
      <PromoCodeSection
        {...defaultProps}
        promoCode="INVALID"
        promoCodeValidation={{ isValid: false, discountAmount: 0, message: 'Invalid promo code' }}
      />
    );
    expect(screen.getByText('Invalid promo code')).toBeInTheDocument();
  });

  it('shows Remove button when promo code is valid', () => {
    render(
      <PromoCodeSection
        {...defaultProps}
        promoCode="SAVE10"
        promoCodeValidation={{ isValid: true, discountAmount: 10.00, message: 'Success!' }}
      />
    );
    expect(screen.getByText('Remove')).toBeInTheDocument();
  });

  it('calls onRemove when Remove button is clicked', () => {
    const onRemove = vi.fn();
    render(
      <PromoCodeSection
        {...defaultProps}
        promoCode="SAVE10"
        promoCodeValidation={{ isValid: true, discountAmount: 10.00, message: 'Success!' }}
        onRemove={onRemove}
      />
    );
    
    fireEvent.click(screen.getByText('Remove'));
    expect(onRemove).toHaveBeenCalledTimes(1);
  });

  it('hides input field when promo code is valid', () => {
    render(
      <PromoCodeSection
        {...defaultProps}
        promoCode="SAVE10"
        promoCodeValidation={{ isValid: true, discountAmount: 10.00, message: 'Success!' }}
      />
    );
    expect(screen.queryByPlaceholderText('Enter promo code')).not.toBeInTheDocument();
  });

  it('converts input to uppercase', () => {
    const onPromoCodeChange = vi.fn();
    render(<PromoCodeSection {...defaultProps} onPromoCodeChange={onPromoCodeChange} />);
    
    const input = screen.getByPlaceholderText('Enter promo code');
    fireEvent.change(input, { target: { value: 'save20' } });
    
    expect(onPromoCodeChange).toHaveBeenCalledWith('SAVE20');
  });
});
