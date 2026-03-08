import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { Provider } from 'react-redux';
import './index.css';
import './shared/i18n'; // Initialize i18n
import App from './App';
import { store } from '@/shared/lib/store';

const rootElement = document.getElementById('root');
if (!rootElement) {
  throw new Error('Root element not found in HTML');
}

createRoot(rootElement).render(
  <StrictMode>
    <Provider store={store}>
      <App />
    </Provider>
  </StrictMode>
);
