import { render, screen } from '@testing-library/react';
import { describe, it, expect } from 'vitest';
import ShippingAddress from './ShippingAddress';

describe('ShippingAddress', () => {
  const mockAddress = {
    firstName: 'John',
    lastName: 'Doe',
    streetLine1: '123 Main St',
    streetLine2: 'Apt 4B',
    city: 'New York',
    state: 'NY',
    postalCode: '10001',
    country: 'United States',
    phone: '+1-555-123-4567',
  };

  it('renders shipping address heading', () => {
    render(<ShippingAddress address={mockAddress} />);

    expect(screen.getByText(/shipping address/i)).toBeInTheDocument();
  });

  it('displays full name', () => {
    render(<ShippingAddress address={mockAddress} />);

    expect(screen.getByText(/john doe/i)).toBeInTheDocument();
  });

  it('displays street address', () => {
    render(<ShippingAddress address={mockAddress} />);

    expect(screen.getByText('123 Main St')).toBeInTheDocument();
  });

  it('displays secondary street line when present', () => {
    render(<ShippingAddress address={mockAddress} />);

    expect(screen.getByText('Apt 4B')).toBeInTheDocument();
  });

  it('displays city, state, and postal code', () => {
    render(<ShippingAddress address={mockAddress} />);

    expect(screen.getByText(/new york, ny 10001/i)).toBeInTheDocument();
  });

  it('displays country', () => {
    render(<ShippingAddress address={mockAddress} />);

    expect(screen.getByText('United States')).toBeInTheDocument();
  });

  it('displays phone number when present', () => {
    render(<ShippingAddress address={mockAddress} />);

    expect(screen.getByText(/\+1-555-123-4567/)).toBeInTheDocument();
  });

  it('does not display phone when not present', () => {
    const addressWithoutPhone = { ...mockAddress, phone: '' };

    render(<ShippingAddress address={addressWithoutPhone} />);

    expect(screen.queryByText(/\+1-555-123-4567/)).not.toBeInTheDocument();
  });

  it('handles address without secondary line', () => {
    const addressWithoutSecondLine = { ...mockAddress, streetLine2: '' };

    render(<ShippingAddress address={addressWithoutSecondLine} />);

    expect(screen.getByText('123 Main St')).toBeInTheDocument();
    expect(screen.queryByText('Apt 4B')).not.toBeInTheDocument();
  });
});
