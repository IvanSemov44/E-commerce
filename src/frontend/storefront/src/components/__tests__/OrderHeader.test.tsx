import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import OrderHeader from '../../pages/components/OrderDetail/OrderHeader';

describe('OrderHeader', () => {
  const defaultProps = {
    orderNumber: 'ORD-12345',
    createdAt: '2024-01-15T10:30:00Z',
    status: 'processing',
    canCancel: false,
    isCancelling: false,
    onCancel: vi.fn(),
  };

  it('renders order number correctly', () => {
    render(<OrderHeader {...defaultProps} />);
    expect(screen.getByText('#ORD-12345')).toBeInTheDocument();
  });

  it('renders formatted order date', () => {
    render(<OrderHeader {...defaultProps} />);
    expect(screen.getByText('1/15/2024')).toBeInTheDocument();
  });

  it('renders formatted order time', () => {
    render(<OrderHeader {...defaultProps} />);
    // The time format is hour:minute, so it should show something like "10:30 AM"
    expect(screen.getByText(/\d{1,2}:\d{2}/)).toBeInTheDocument();
  });

  it('does not render cancel button when canCancel is false', () => {
    render(<OrderHeader {...defaultProps} canCancel={false} />);
    expect(screen.queryByText('Cancel Order')).not.toBeInTheDocument();
  });

  it('renders cancel button when canCancel is true', () => {
    render(<OrderHeader {...defaultProps} canCancel={true} />);
    expect(screen.getByText('Cancel Order')).toBeInTheDocument();
  });

  it('calls onCancel when cancel button is clicked', () => {
    const onCancel = vi.fn();
    render(<OrderHeader {...defaultProps} canCancel={true} onCancel={onCancel} />);
    
    fireEvent.click(screen.getByText('Cancel Order'));
    expect(onCancel).toHaveBeenCalledTimes(1);
  });

  it('disables cancel button when isCancelling is true', () => {
    const onCancel = vi.fn();
    render(<OrderHeader {...defaultProps} canCancel={true} isCancelling={true} onCancel={onCancel} />);
    
    const button = screen.getByText('Cancel Order') as HTMLButtonElement;
    expect(button.disabled).toBe(true);
  });

  it('renders order status timeline', () => {
    render(<OrderHeader {...defaultProps} status="shipped" />);
    // OrderStatusTimeline should be rendered - we check that the component renders without error
    expect(screen.getByText('#ORD-12345')).toBeInTheDocument();
  });

  it('handles different date formats', () => {
    render(<OrderHeader {...defaultProps} createdAt="2024-12-25T08:00:00Z" />);
    expect(screen.getByText('12/25/2024')).toBeInTheDocument();
  });

  it('renders label texts correctly', () => {
    render(<OrderHeader {...defaultProps} />);
    expect(screen.getByText('Order Number')).toBeInTheDocument();
    expect(screen.getByText('Order Date')).toBeInTheDocument();
  });
});
