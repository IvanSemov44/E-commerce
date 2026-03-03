/**
 * Shared UI Components Barrel Export
 * 
 * Base UI components used across features:
 * - Button, Input, Card - form controls and layout
 * - Pagination, Skeleton - data display and loading states
 * - EmptyState - placeholder content
 * 
 * These are truly shared, reusable UI primitives.
 */

export { Button, type ButtonProps } from './Button';
export { Input, type InputProps } from './Input';
export { Card, type CardProps } from './Card';
export { default as Pagination } from './Pagination';
export { default as Skeleton, ProductCardSkeleton, CartItemSkeleton, OrderCardSkeleton, ProductGridSkeleton, CartSkeleton, OrderHistorySkeleton } from './Skeleton';
export { default as EmptyState, EmptyCart, EmptyWishlist, EmptyOrders, NoSearchResults, ErrorState } from './EmptyState';
