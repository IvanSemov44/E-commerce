/**
 * App-Level Components Barrel Export
 *
 * Components that are only used by app-level files (AppShell, AppRoutes, AppProviders).
 * These should NOT be imported from features — they belong to the app shell layer.
 *
 * Usage:
 *   import { Header, Footer } from '@/app/layouts';
 *   import { AnnouncementBar } from '@/app/AnnouncementBar';
 */

// Layout Components
export { Header, Footer } from './layouts';

// App Shell Components
export { AnnouncementBar } from './AnnouncementBar';
export { CookieConsent } from './CookieConsent';
export { ErrorBoundary } from './ErrorBoundary';
export { ErrorPage } from './ErrorPage';
// UI Utility Components (app-level)
export { ThemeToggle } from './ThemeToggle';
export { LanguageSwitcher } from './LanguageSwitcher';
export { SearchBar } from './SearchBar';

// Toast
export { Toast, ToastContainer, useToast } from './Toast';
