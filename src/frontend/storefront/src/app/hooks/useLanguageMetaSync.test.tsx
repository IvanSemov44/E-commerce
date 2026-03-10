import { renderHook } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { useLanguageMetaSync } from './useLanguageMetaSync';

const { onMock, offMock, getLanguageDirectionMock, i18nMock } = vi.hoisted(() => {
  const on = vi.fn();
  const off = vi.fn();
  const getDirection = vi.fn((lang: 'en' | 'bg') => (lang === 'bg' ? 'rtl' : 'ltr'));

  return {
    onMock: on,
    offMock: off,
    getLanguageDirectionMock: getDirection,
    i18nMock: {
      resolvedLanguage: 'en',
      language: 'en',
      on,
      off,
    },
  };
});

vi.mock('@/shared/i18n', () => ({
  __esModule: true,
  default: i18nMock,
  getLanguageDirection: getLanguageDirectionMock,
}));

function resetDocumentLanguageMeta() {
  document.documentElement.lang = '';
  document.documentElement.dir = '';
}

describe('useLanguageMetaSync', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    i18nMock.resolvedLanguage = 'en';
    i18nMock.language = 'en';
    resetDocumentLanguageMeta();
  });

  afterEach(() => {
    resetDocumentLanguageMeta();
  });

  it('applies initial language metadata from resolvedLanguage', () => {
    renderHook(() => useLanguageMetaSync());

    expect(getLanguageDirectionMock).toHaveBeenCalledWith('en');
    expect(document.documentElement.lang).toBe('en');
    expect(document.documentElement.dir).toBe('ltr');
  });

  it('falls back to language when resolvedLanguage is empty', () => {
    i18nMock.resolvedLanguage = '';
    i18nMock.language = 'bg';

    renderHook(() => useLanguageMetaSync());

    expect(getLanguageDirectionMock).toHaveBeenCalledWith('bg');
    expect(document.documentElement.lang).toBe('bg');
    expect(document.documentElement.dir).toBe('rtl');
  });

  it('subscribes to and handles languageChanged events', () => {
    renderHook(() => useLanguageMetaSync());

    expect(onMock).toHaveBeenCalledTimes(1);
    expect(onMock).toHaveBeenCalledWith('languageChanged', expect.any(Function));

    const handler = onMock.mock.calls[0][1] as (lang: string) => void;
    handler('bg');

    expect(getLanguageDirectionMock).toHaveBeenCalledWith('bg');
    expect(document.documentElement.lang).toBe('bg');
    expect(document.documentElement.dir).toBe('rtl');
  });

  it('unsubscribes the exact listener on unmount', () => {
    const { unmount } = renderHook(() => useLanguageMetaSync());

    const handler = onMock.mock.calls[0][1] as (lang: string) => void;
    unmount();

    expect(offMock).toHaveBeenCalledTimes(1);
    expect(offMock).toHaveBeenCalledWith('languageChanged', handler);
  });
});
