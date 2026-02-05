// Pagination
export const DEFAULT_PAGE_SIZE = 10;
export const DEFAULT_ORDERS_PAGE_SIZE = 10;
export const DEFAULT_PRODUCTS_PAGE_SIZE = 10;
export const DEFAULT_REVIEWS_PAGE_SIZE = 10;

// Status Colors
export const STATUS_COLORS = {
  pending: '#f59e0b',
  processing: '#3b82f6',
  shipped: '#8b5cf6',
  delivered: '#10b981',
  cancelled: '#ef4444',
} as const;

// Order Status
export const ORDER_STATUSES = [
  'Pending',
  'Processing',
  'Shipped',
  'Delivered',
  'Cancelled',
] as const;

// Promo Code Types
export const PROMO_CODE_TYPES = [
  'Percentage',
  'Fixed Amount',
] as const;

// Stock Thresholds
export const LOW_STOCK_THRESHOLD = 10;
export const OUT_OF_STOCK_THRESHOLD = 0;

// Date Formats
export const DATE_FORMAT = 'MMM dd, yyyy';
export const DATETIME_FORMAT = 'MMM dd, yyyy HH:mm';
