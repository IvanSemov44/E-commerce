import en from './en.json';
import bg from './bg.json';

export const resources = {
  en: { translation: en },
  bg: { translation: bg },
} as const;

export type Language = keyof typeof resources;
export type TranslationKeys = typeof en;

export const languages: { code: Language; name: string; nativeName: string; dir: 'ltr' | 'rtl' }[] =
  [
    { code: 'en', name: 'English', nativeName: 'English', dir: 'ltr' },
    { code: 'bg', name: 'Bulgarian', nativeName: 'Български', dir: 'ltr' },
  ];

export const defaultLanguage: Language = 'en';
