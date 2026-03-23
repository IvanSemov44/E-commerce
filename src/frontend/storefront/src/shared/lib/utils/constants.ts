// Images
export const DEFAULT_PRODUCT_IMAGE =
  'data:image/svg+xml,%3Csvg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 300 300"%3E%3Crect fill="%23e5e7eb" width="300" height="300"/%3E%3Ctext x="50%25" y="50%25" dominant-baseline="middle" text-anchor="middle" font-family="system-ui" font-size="16" fill="%236b7280"%3ENo Image%3C/text%3E%3C/svg%3E';

// Pagination
export const DEFAULT_PRODUCTS_PAGE_SIZE = 12;

// Shipping
export const FREE_SHIPPING_THRESHOLD = 100;
export const STANDARD_SHIPPING_COST = 10;

// Tax
export const DEFAULT_TAX_RATE = 0.08;

// Progressive bootstrap loading thresholds (milliseconds)
// Phase 0: 0–TOP_BAR_DELAY_MS     → nothing shown (80% of loads finish here)
// Phase 1: TOP_BAR_DELAY_MS–FULL_FALLBACK_DELAY_MS → top loading bar only
// Phase 2: >FULL_FALLBACK_DELAY_MS → full loading fallback
export const BOOTSTRAP_TOP_BAR_DELAY_MS = 150;
export const BOOTSTRAP_FULL_FALLBACK_DELAY_MS = 900;
