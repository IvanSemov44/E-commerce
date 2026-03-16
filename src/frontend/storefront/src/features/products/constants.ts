export const PRODUCTS_PAGE_SIZE = 12;
export const FEATURED_PRODUCTS_PAGE_SIZE = 10;
export const DEFAULT_PRODUCTS_PAGE_SIZE = 20; // API fallback — ProductsPage uses PRODUCTS_PAGE_SIZE (12)
export const ADDED_TO_CART_RESET_MS = 2000;
export const REVIEW_SKELETON_COUNT = 3;

export const VALID_SORT_BY = ['newest', 'name', 'price-asc', 'price-desc', 'rating'] as const;
export type SortBy = (typeof VALID_SORT_BY)[number];
