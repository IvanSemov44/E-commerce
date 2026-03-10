import { Suspense } from 'react';
import LoadingFallback from '@/shared/components/LoadingFallback';
import AppShell from '@/app/AppShell';
import AppRoutes from '@/app/AppRoutes';
import { useAppInitialization, useLanguageMetaSync, useRouteTelemetry } from '@/app/hooks';

function App() {
  useRouteTelemetry();
  useLanguageMetaSync();
  const { isInitializing } = useAppInitialization();

  return (
    <AppShell isInitializing={isInitializing}>
      <Suspense fallback={<LoadingFallback />}>
        <AppRoutes />
      </Suspense>
    </AppShell>
  );
}

export default App;
