import { createSlice, createAsyncThunk } from '@reduxjs/toolkit';
import type { Language } from '@/shared/i18n';
import { changeLanguage, getLanguageDirection, defaultLanguage } from '@/shared/i18n';

export interface LanguageState {
  currentLanguage: Language;
  direction: 'ltr' | 'rtl';
  initialized: boolean;
}

const storedLanguage = (typeof window !== 'undefined' 
  ? localStorage.getItem('language') as Language 
  : null) || defaultLanguage;

const initialState: LanguageState = {
  currentLanguage: storedLanguage,
  direction: getLanguageDirection(storedLanguage),
  initialized: false,
};

/**
 * Async thunk for changing language
 * Handles both i18n change and localStorage persistence
 */
export const setLanguage = createAsyncThunk(
  'language/set',
  async (language: Language) => {
    await changeLanguage(language);
    return language;
  }
);

export const languageSlice = createSlice({
  name: 'language',
  initialState,
  reducers: {
    initializeLanguage: (state) => {
      const storedLang = (typeof window !== 'undefined' 
        ? localStorage.getItem('language') as Language 
        : null) || defaultLanguage;
      
      state.currentLanguage = storedLang;
      state.direction = getLanguageDirection(storedLang);
      state.initialized = true;
      
      // Apply direction to document
      if (typeof document !== 'undefined') {
        document.documentElement.dir = state.direction;
        document.documentElement.lang = storedLang;
      }
    },
  },
  extraReducers: (builder) => {
    builder.addCase(setLanguage.fulfilled, (state, action) => {
      state.currentLanguage = action.payload;
      state.direction = getLanguageDirection(action.payload);
    });
  },
});

export const { initializeLanguage } = languageSlice.actions;

// Selectors
export const selectCurrentLanguage = (state: { language: LanguageState }) => 
  state.language.currentLanguage;

export const selectLanguageDirection = (state: { language: LanguageState }) => 
  state.language.direction;

export const selectLanguageInitialized = (state: { language: LanguageState }) => 
  state.language.initialized;

export const languageReducer = languageSlice.reducer;
