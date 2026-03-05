import type { OrderHeaderProps } from './OrderHeader.types';

/**
 * Get CSS class for status badge based on order status
 * @param status - Order status
 * @returns CSS class string with background and text color
 */
export function getStatusBadgeColor(status: OrderHeaderProps['status']): string {
  const statusColors: Record<string, string> = {
    Pending: 'bg-yellow-100 text-yellow-800',
    Processing: 'bg-blue-100 text-blue-800',
    Shipped: 'bg-purple-100 text-purple-800',
    Delivered: 'bg-green-100 text-green-800',
    Cancelled: 'bg-red-100 text-red-800',
  };

  return statusColors[status] || 'bg-gray-100 text-gray-800';
}

/**
 * Check if order can be cancelled based on status
 * @param status - Order status
 * @returns Boolean indicating if order can be cancelled
 */
export function canCancelOrder(status: OrderHeaderProps['status']): boolean {
  const cancellableStatuses: OrderHeaderProps['status'][] = ['Pending', 'Processing'];
  return cancellableStatuses.includes(status);
}
