import { BrowserRouter as Router } from 'react-router-dom';
import { Suspense } from 'react';
import { Toaster } from 'react-hot-toast';
import ToastContainer from '@/shared/components/Toast/ToastContainer';
import ErrorBoundary from '@/shared/components/ErrorBoundary';
import LoadingFallback from '@/shared/components/LoadingFallback';
import AppInitializer from '@/app/AppInitializer';
import AppShell from '@/app/AppShell';
import AppRoutes from '@/app/AppRoutes';

function App() {
  return (
    <Router>
      <ErrorBoundary>
        <Toaster
          position="top-right"
          toastOptions={{
            style: {
              marginTop: '80px',
            },
          }}
        />
        <ToastContainer />
        <AppInitializer>
          {({ isInitializing }) => (
            <AppShell isInitializing={isInitializing}>
              <Suspense fallback={<LoadingFallback />}>
                <AppRoutes />
              </Suspense>
            </AppShell>
          )}
        </AppInitializer>
      </ErrorBoundary>
    </Router>
  );
}

export default App;
