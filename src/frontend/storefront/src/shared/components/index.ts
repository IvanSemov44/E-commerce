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
export { default as ErrorAlert } from './ErrorAlert';
export { default as OptimizedImage } from './OptimizedImage';
export { default as PageHeader } from './PageHeader';
export { default as PaginatedView } from './Pagination';
export { default as QueryRenderer } from './QueryRenderer';
export { default as TrustSignals } from './TrustSignals';
