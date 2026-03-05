import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, it, expect, vi } from 'vitest';
import OrderHeader from './OrderHeader';

describe('OrderHeader', () => {
  it('renders order number and title', () => {
    render(
      <OrderHeader
        orderNumber="ORD-001"
        createdAt="2024-01-15T10:30:00Z"
        status="Delivered"
        canCancel={false}
        isCancelling={false}
        onCancel={vi.fn()}
      />
    );

    expect(screen.getByText(/#ORD-001/)).toBeInTheDocument();
  });

  it('displays order creation date', () => {
    render(
      <OrderHeader
        orderNumber="ORD-001"
        createdAt="2024-01-15T10:30:00Z"
        status="Delivered"
        canCancel={false}
        isCancelling={false}
        onCancel={vi.fn()}
      />
    );

    const dateElement = screen.getByText(/1\/15\/2024/i);
    expect(dateElement).toBeInTheDocument();
  });

  it('displays status badge', () => {
    render(
      <OrderHeader
        orderNumber="ORD-001"
        createdAt="2024-01-15T10:30:00Z"
        status="Delivered"
        canCancel={false}
        isCancelling={false}
        onCancel={vi.fn()}
      />
    );

    expect(screen.getByText(/delivered/i)).toBeInTheDocument();
  });

  it('shows cancel button when canCancel is true', () => {
    render(
      <OrderHeader
        orderNumber="ORD-001"
        createdAt="2024-01-15T10:30:00Z"
        status="Pending"
        canCancel={true}
        isCancelling={false}
        onCancel={vi.fn()}
      />
    );

    expect(screen.getByRole('button', { name: /cancel/i })).toBeInTheDocument();
  });

  it('hides cancel button when canCancel is false', () => {
    render(
      <OrderHeader
        orderNumber="ORD-001"
        createdAt="2024-01-15T10:30:00Z"
        status="Delivered"
        canCancel={false}
        isCancelling={false}
        onCancel={vi.fn()}
      />
    );

    expect(screen.queryByRole('button', { name: /cancel order/i })).not.toBeInTheDocument();
  });

  it('calls onCancel when cancel button clicked', async () => {
    const user = userEvent.setup();
    const onCancel = vi.fn();

    render(
      <OrderHeader
        orderNumber="ORD-001"
        createdAt="2024-01-15T10:30:00Z"
        status="Pending"
        canCancel={true}
        isCancelling={false}
        onCancel={onCancel}
      />
    );

    const cancelButton = screen.getByRole('button', { name: /cancel/i });
    await user.click(cancelButton);

    expect(onCancel).toHaveBeenCalled();
  });

  it('disables cancel button while cancelling', () => {
    render(
      <OrderHeader
        orderNumber="ORD-001"
        createdAt="2024-01-15T10:30:00Z"
        status="Pending"
        canCancel={true}
        isCancelling={true}
        onCancel={vi.fn()}
      />
    );

    const cancelButton = screen.getByRole('button', { name: /cancelling/i });
    expect(cancelButton).toBeDisabled();
  });
});
