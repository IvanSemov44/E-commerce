import i18n from 'i18next';
import { initReactI18next } from 'react-i18next';
import LanguageDetector from 'i18next-browser-languagedetector';

import { resources, defaultLanguage, languages, type Language } from './locales';

/**
 * i18n Configuration
 * 
 * Provides internationalization support with:
 * - Automatic language detection from browser/localStorage
 * - Lazy loading of translations
 * - RTL support for future languages
 * - Redux integration for state management
 */
export const i18nConfig = {
  resources,
  fallbackLng: defaultLanguage,
  defaultNS: 'translation',
  ns: ['translation'],

  // Language detection configuration
  detection: {
    order: ['localStorage', 'navigator', 'htmlTag'],
    caches: ['localStorage'],
    lookupLocalStorage: 'language',
  },

  // React configuration
  react: {
    useSuspense: false,
    bindI18nStore: 'added',
  },

  // Interpolation configuration
  interpolation: {
    escapeValue: false, // React already escapes by default
  },

  // Backend configuration (for lazy loading)
  backend: {
    loadPath: '/locales/{{lng}}/{{ns}}.json',
  },
};

// Initialize i18next
i18n
  .use(LanguageDetector)
  .use(initReactI18next)
  .init(i18nConfig);

/**
 * Helper to get language direction
 */
const getLanguageDir = (lang: Language): 'ltr' | 'rtl' => {
  const langConfig = languages.find(l => l.code === lang);
  return langConfig?.dir || 'ltr';
};

/**
 * Helper to change language with Redux integration
 */
const changeLang = async (lang: Language): Promise<void> => {
  await i18n.changeLanguage(lang);
  localStorage.setItem('language', lang);
  
  // Update document direction for RTL support
  const dir = getLanguageDir(lang);
  document.documentElement.dir = dir;
  document.documentElement.lang = lang;
};

export { languages, defaultLanguage, resources };
export { getLanguageDir as getLanguageDirection, changeLang as changeLanguage };
export default i18n;
export type { Language };
