import { useTranslation as useI18nextTranslation } from 'react-i18next';

/**
 * Custom hook for translations
 * Wraps i18next with convenience helpers
 */
export const useTranslation = () => {
  const { t, i18n } = useI18nextTranslation();

  const changeLanguage = async (lang: string) => {
    await i18n.changeLanguage(lang);
  };

  return {
    t,
    i18n,
    currentLanguage: i18n.language,
    changeLanguage,
    isEnglish: i18n.language === 'en',
    isBulgarian: i18n.language === 'bg',
  };
};
