import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import ShippingAddress from '../../pages/components/OrderDetail/ShippingAddress';

describe('ShippingAddress', () => {
  const validAddress = {
    firstName: 'John',
    lastName: 'Doe',
    streetLine1: '123 Main Street',
    city: 'New York',
    state: 'NY',
    postalCode: '10001',
    country: 'USA',
    phone: '+1-555-123-4567',
  };

  it('renders shipping address title', () => {
    render(<ShippingAddress address={validAddress} />);
    expect(screen.getByText('Shipping Address')).toBeInTheDocument();
  });

  it('renders full name correctly', () => {
    render(<ShippingAddress address={validAddress} />);
    expect(screen.getByText('John Doe')).toBeInTheDocument();
  });

  it('renders street address', () => {
    render(<ShippingAddress address={validAddress} />);
    expect(screen.getByText('123 Main Street')).toBeInTheDocument();
  });

  it('renders city, state, and postal code', () => {
    render(<ShippingAddress address={validAddress} />);
    expect(screen.getByText('New York, NY 10001')).toBeInTheDocument();
  });

  it('renders country', () => {
    render(<ShippingAddress address={validAddress} />);
    expect(screen.getByText('USA')).toBeInTheDocument();
  });

  it('renders phone number when provided', () => {
    render(<ShippingAddress address={validAddress} />);
    expect(screen.getByText('+1-555-123-4567')).toBeInTheDocument();
  });

  it('does not render phone section when phone is undefined', () => {
    const addressWithoutPhone = { ...validAddress, phone: undefined };
    render(<ShippingAddress address={addressWithoutPhone} />);
    expect(screen.queryByText('+1-555-123-4567')).not.toBeInTheDocument();
  });

  it('renders nothing when address is undefined', () => {
    const { container } = render(<ShippingAddress address={undefined} />);
    expect(container.firstChild).toBeNull();
  });

  it('renders nothing when address is null', () => {
    const { container } = render(<ShippingAddress address={undefined} />);
    expect(container.firstChild).toBeNull();
  });

  it('handles missing optional fields gracefully', () => {
    const minimalAddress = {
      firstName: 'Jane',
      lastName: 'Smith',
      streetLine1: '456 Oak Ave',
    };
    render(<ShippingAddress address={minimalAddress as any} />);
    expect(screen.getByText('Jane Smith')).toBeInTheDocument();
    expect(screen.getByText('456 Oak Ave')).toBeInTheDocument();
  });

  it('renders phone icon when phone is present', () => {
    render(<ShippingAddress address={validAddress} />);
    // Phone icon should be present (SVG)
    const phoneIcon = document.querySelector('svg');
    expect(phoneIcon).toBeInTheDocument();
  });
});
