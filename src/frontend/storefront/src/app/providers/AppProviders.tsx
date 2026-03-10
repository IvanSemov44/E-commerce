import type { ReactNode } from 'react';
import { Provider } from 'react-redux';
import { BrowserRouter } from 'react-router-dom';
import ToastContainer from '@/shared/components/Toast/ToastContainer';
import CookieConsent from '@/shared/components/CookieConsent';
import ErrorBoundary from '@/shared/components/ErrorBoundary';
import { store } from '@/shared/lib/store';
import { ThemeProvider } from './ThemeProvider';

interface AppProvidersProps {
  children: ReactNode;
}

export default function AppProviders({ children }: AppProvidersProps) {
  return (
    <ErrorBoundary>
      <Provider store={store}>
        <BrowserRouter>
          <ThemeProvider>
            {children}
            <ToastContainer />
            <CookieConsent />
          </ThemeProvider>
        </BrowserRouter>
      </Provider>
    </ErrorBoundary>
  );
}
