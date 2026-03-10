import { Suspense } from 'react';
import { RouteLoadingFallback } from '@/app/skeletons';
import AppShell from '@/app/AppShell';
import AppRoutes from '@/app/AppRoutes';
import { useAppInitialization, useLanguageMetaSync, useRouteTelemetry } from '@/app/hooks';

function App() {
  useRouteTelemetry();
  useLanguageMetaSync();
  const { isInitializing } = useAppInitialization();

  return (
    <AppShell isInitializing={isInitializing}>
      <Suspense fallback={<RouteLoadingFallback />}>
        <AppRoutes />
      </Suspense>
    </AppShell>
  );
}

export default App;
