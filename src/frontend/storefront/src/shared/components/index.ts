/**
 * Shared Components Barrel Export
 *
 * All shared components used across features:
 * - UI components (Button, Input, Card, etc)
 * - Utility components (EmptyState, LoadingSkeleton, etc)
 *
 * Usage:
 *   import { Button } from '@/shared/components';
 */

// UI Components
export { Button } from './ui';
export { Input } from './ui';
export { Card } from './ui';

// Skeletons and loading components
export * from './Skeletons';

// Utility Components
export { EmptyState } from './ui/EmptyState';
export { ErrorAlert } from './ErrorAlert';
export { default as OptimizedImage } from './OptimizedImage';
export { default as PageHeader } from './PageHeader';
export { default as PaginatedView } from './Pagination';
export { default as QueryRenderer } from './QueryRenderer';
export { TrustSignals } from './TrustSignals';

// Order display components (shared across cart, checkout, orders)
export { OrderTotalsDisplay } from './OrderTotalsDisplay/OrderTotalsDisplay';
export { OrderTotals } from './OrderTotals';
export type { OrderTotalsProps } from './OrderTotals';
