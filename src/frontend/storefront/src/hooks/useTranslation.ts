import { useTranslation as useI18nextTranslation } from 'react-i18next';
import { useSelector, useDispatch } from 'react-redux';
import type { TypedUseSelectorHook } from 'react-redux';
import type { RootState, AppDispatch } from '../store/store';
import { setLanguage, selectCurrentLanguage } from '../store/slices/languageSlice';
import type { Language } from '../i18n';

// Typed Redux hooks
export const useAppDispatch: () => AppDispatch = useDispatch;
export const useAppSelector: TypedUseSelectorHook<RootState> = useSelector;

/**
 * Custom hook for translations with Redux integration
 * Provides both translation function and language switching
 */
export const useTranslation = () => {
  const { t, i18n } = useI18nextTranslation();
  const dispatch = useAppDispatch();
  const currentLanguage = useAppSelector(selectCurrentLanguage);

  const changeLanguage = async (lang: Language) => {
    await i18n.changeLanguage(lang);
    await dispatch(setLanguage(lang));
  };

  return {
    t,
    i18n,
    currentLanguage,
    changeLanguage,
    isEnglish: currentLanguage === 'en',
    isBulgarian: currentLanguage === 'bg',
  };
};
