/**
 * Shared Components Barrel Export
 * 
 * All shared components used across features:
 * - UI components (Button, Input, Card, etc)
 * - Layout components (Header, Footer)
 * - Utility components (EmptyState, LoadingSkeleton, etc)
 * 
 * Usage:
 *   import { Button, Header, Footer } from '@/shared/components';
 */

// UI Components
export { Button, type ButtonProps } from './ui';
export { Input, type InputProps } from './ui';
export { Card } from './ui';
export { Skeleton } from './ui';

// Skeletons
export {
	ProductSkeleton,
	ProductsGridSkeleton,
	ProfileSkeleton,
	CartSkeleton,
} from './Skeletons';

// Utility Components
export { default as AnnouncementBar } from './AnnouncementBar';
export { default as CategoryFilter } from './CategoryFilter';
export { default as CookieConsent } from './CookieConsent';
export { default as EmptyState } from './EmptyState';
export { default as ErrorAlert } from './ErrorAlert';
export { default as ErrorBoundary } from './ErrorBoundary';
export { default as ErrorPage } from './ErrorPage';
export { default as LanguageSwitcher } from './LanguageSwitcher';
export { default as LoadingFallback } from './LoadingFallback';
export { default as LoadingSkeleton } from './LoadingSkeleton';
export { default as OptimizedImage } from './OptimizedImage';
export { default as PageHeader } from './PageHeader';
export { default as PaginatedView } from './Pagination';
export { default as ProtectedRoute } from './ProtectedRoute';
export { default as QueryRenderer } from './QueryRenderer';
export { default as ReviewForm } from './ReviewForm';
export { default as ReviewList } from './ReviewList';
export { default as SearchBar } from './SearchBar';
export { default as StarRating } from './StarRating';
export { default as ThemeToggle } from './ThemeToggle';
export { Toast, ToastContainer } from './Toast';
export { default as TrustSignals } from './TrustSignals';

// Layout Components
export { Header, Footer } from './layouts';
