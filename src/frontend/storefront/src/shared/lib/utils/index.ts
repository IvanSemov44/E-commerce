// Utility functions and constants
export {
  DEFAULT_PRODUCT_IMAGE,
  DEFAULT_PRODUCTS_PAGE_SIZE,
  FREE_SHIPPING_THRESHOLD,
  STANDARD_SHIPPING_COST,
  DEFAULT_TAX_RATE,
  BOOTSTRAP_TOP_BAR_DELAY_MS,
  BOOTSTRAP_FULL_FALLBACK_DELAY_MS,
} from './constants';
export { parseBackendFieldErrors } from './parseBackendFieldErrors';
export { logger } from './logger';
export { telemetry } from './telemetry';
export { formatPrice, formatPriceLocale } from './priceFormatter';
export { type OrderTotals, calculateOrderTotals } from './orderCalculations';
export { isApiError } from './errorUtils';
