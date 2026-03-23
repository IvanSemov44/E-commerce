import { Suspense } from 'react';
import { Outlet, Scripts, ScrollRestoration } from 'react-router';
import { Provider } from 'react-redux';
import { store } from '@/shared/lib/store';
import { ThemeProvider } from '@/app/providers/ThemeProvider';
import { ErrorBoundary } from '@/app/ErrorBoundary';
import { ToastContainer } from '@/app/Toast/ToastContainer';
import { CookieConsent } from '@/app/CookieConsent';
import { AppShell } from '@/app/AppShell';
import { RouteLoadingFallback } from '@/app/skeletons';
import { useAppInitialization, useLanguageMetaSync, useRouteTelemetry } from '@/app/hooks';

function AppRoot() {
  useRouteTelemetry();
  useLanguageMetaSync();
  const { isInitializing } = useAppInitialization();

  return (
    <AppShell isInitializing={isInitializing}>
      <Suspense fallback={<RouteLoadingFallback />}>
        <Outlet />
      </Suspense>
    </AppShell>
  );
}

export default function Root() {
  return (
    <ErrorBoundary>
      <Provider store={store}>
        <ThemeProvider>
          <AppRoot />
          <ToastContainer />
          <CookieConsent />
          <ScrollRestoration />
          <Scripts />
        </ThemeProvider>
      </Provider>
    </ErrorBoundary>
  );
}
