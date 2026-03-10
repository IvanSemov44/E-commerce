import { useEffect } from 'react';
import i18n, { getLanguageDirection } from '@/shared/i18n';

/**
 * Synchronizes i18n language changes to document metadata
 * Updates document.lang and document.dir attributes when language changes
 */
export function useLanguageMetaSync() {
  useEffect(() => {
    const applyLanguageMeta = (lang: string) => {
      const direction = getLanguageDirection(lang as 'en' | 'bg');
      document.documentElement.lang = lang;
      document.documentElement.dir = direction;
    };

    // Apply initial language
    applyLanguageMeta(i18n.resolvedLanguage || i18n.language || 'en');

    // Listen for language changes
    i18n.on('languageChanged', applyLanguageMeta);

    return () => {
      i18n.off('languageChanged', applyLanguageMeta);
    };
  }, []);
}
