/**
 * Format date to locale string
 * @param dateString - ISO date string
 * @returns Formatted date
 */
export function formatOrderDate(dateString: string): string {
  const orderDate = new Date(dateString);
  return orderDate.toLocaleDateString('en-US', {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
  });
}

/**
 * Get CSS class for order status
 * @param status - Order status
 * @param styles - CSS module styles object
 * @returns Status class name
 */
export function getStatusClassName(status: string, styles: Record<string, string>): string {
  const statusLower = status.toLowerCase();
  const className = `status${status[0].toUpperCase() + statusLower.slice(1)}`;
  return styles[className] || styles.statusPending;
}

/**
 * Format items label with count
 * @param count - Number of items
 * @param t - Translation function
 * @returns Formatted items label
 */
export function formatItemsLabel(count: number, t: (key: string, options?: Record<string, unknown>) => string): string {
  return count === 1
    ? t('orders.oneItem') || '1 item'
    : t('orders.multipleItems', { count }) || `${count} items`;
}

/**
 * Format item names preview
 * @param items - Order items
 * @param maxDisplay - Maximum items to display
 * @param t - Translation function
 * @returns Formatted items string
 */
export function formatItemsPreview(
  items: { productName: string }[],
  maxDisplay: number,
  t: (key: string, options?: Record<string, unknown>) => string
): string {
  if (items.length === 0) return '';
  
  const displayItems = items.slice(0, maxDisplay).map((item) => item.productName).join(', ');
  const remaining = items.length - maxDisplay;
  
  if (remaining > 0) {
    return `${displayItems} +${remaining} ${t('orders.more') || 'more'}`;
  }
  
  return displayItems;
}
