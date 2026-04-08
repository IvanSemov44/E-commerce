import '@testing-library/jest-dom';
import { server } from './msw-server';
import i18n from 'i18next';

// MSW lifecycle — intercept all HTTP at the network level
// onUnhandledRequest: 'warn' during initial setup (change to 'error' once all tests use MSW)
beforeAll(() => server.listen({ onUnhandledRequest: 'warn' }));
afterEach(() => server.resetHandlers());
afterAll(() => server.close());
import { initReactI18next } from 'react-i18next';

// Import actual translations from locale files
import en from '@/shared/i18n/locales/en.json';
import bg from '@/shared/i18n/locales/bg.json';

// Initialize i18next for testing with actual translations
i18n.use(initReactI18next).init({
  resources: {
    en: { translation: en },
    bg: { translation: bg },
  },
  lng: 'en',
  fallbackLng: 'en',
  interpolation: {
    escapeValue: false,
  },
});

// Mock window.matchMedia (not available in jsdom)
Object.defineProperty(window, 'matchMedia', {
  writable: true,
  value: (query: string) => ({
    matches: false,
    media: query,
    onchange: null,
    addListener: () => {}, // deprecated
    removeListener: () => {}, // deprecated
    addEventListener: () => {},
    removeEventListener: () => {},
    dispatchEvent: () => false,
  }),
});

// Mock IntersectionObserver (not available in jsdom)
globalThis.IntersectionObserver = class IntersectionObserver {
  constructor() {}
  disconnect() {}
  observe() {}
  takeRecords() {
    return [];
  }
  unobserve() {}
} as unknown as typeof IntersectionObserver;
