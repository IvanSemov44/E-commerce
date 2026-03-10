/**
 * App-Level Components Barrel Export
 *
 * Components that are only used by app-level files (AppShell, AppRoutes, AppProviders).
 * These should NOT be imported from features — they belong to the app shell layer.
 *
 * Usage:
 *   import { Header, Footer } from '@/app/layouts';
 *   import AnnouncementBar from '@/app/AnnouncementBar';
 */

// Layout Components
export { Header, Footer } from './layouts';

// App Shell Components
export { default as AnnouncementBar } from './AnnouncementBar';
export { default as CookieConsent } from './CookieConsent';
export { default as ErrorBoundary } from './ErrorBoundary';
export { default as ErrorPage } from './ErrorPage';
export { default as ProtectedRoute } from './ProtectedRoute';

// UI Utility Components (app-level)
export { ThemeToggle } from './ThemeToggle';
export { LanguageSwitcher } from './LanguageSwitcher';
export { SearchBar } from './SearchBar';

// Toast
export { Toast, ToastContainer, useToast } from './Toast';
