import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import './index.css';
import './shared/i18n'; // Initialize i18n
import App from './App';
import AppProviders from '@/app/providers/AppProviders';

const rootElement = document.getElementById('root');
if (!rootElement) {
  throw new Error('Root element not found in HTML');
}

createRoot(rootElement).render(
  <StrictMode>
    <AppProviders>
      <App />
    </AppProviders>
  </StrictMode>
);
