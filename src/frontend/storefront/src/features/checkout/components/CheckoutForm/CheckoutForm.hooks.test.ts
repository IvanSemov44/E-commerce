import { describe, it, expect, vi, beforeEach } from 'vitest';
import { renderHookWithProviders } from '@/shared/lib/test/test-utils';
import { useCheckoutForm } from './CheckoutForm.hooks';
import * as localStorageHook from '@/shared/hooks/useLocalStorage';

// ── Module mocks ──────────────────────────────────────────────────────────────

vi.mock('@/shared/hooks/useLocalStorage', () => ({
  useLocalStorage: vi.fn(() => [{}, vi.fn()]),
}));

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string) => key }),
}));

const mockOnSubmit = vi.fn();

const authenticatedState = {
  auth: {
    isAuthenticated: true,
    user: {
      id: 'u1',
      firstName: 'Jane',
      lastName: 'Doe',
      email: 'jane@example.com',
      phone: '+1234567890',
      role: 'customer',
    },
    token: 'tok',
    refreshToken: 'ref',
  },
};

const guestState = {
  auth: { isAuthenticated: false, user: null, token: null, refreshToken: null },
};

// ── Tests ─────────────────────────────────────────────────────────────────────

describe('useCheckoutForm', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(localStorageHook.useLocalStorage).mockReturnValue([{}, vi.fn()] as never);
  });

  // ── Initial values priority ──────────────────────────────────────────────

  it('initialises with empty form for unauthenticated user with no draft', () => {
    const { result } = renderHookWithProviders(() => useCheckoutForm({ onSubmit: mockOnSubmit }), {
      preloadedState: guestState,
    });

    expect(result.current.form.values.firstName).toBe('');
    expect(result.current.form.values.email).toBe('');
  });

  it('pre-fills contact fields from user profile when authenticated and no draft', () => {
    const { result } = renderHookWithProviders(() => useCheckoutForm({ onSubmit: mockOnSubmit }), {
      preloadedState: authenticatedState,
    });

    expect(result.current.form.values.firstName).toBe('Jane');
    expect(result.current.form.values.lastName).toBe('Doe');
    expect(result.current.form.values.email).toBe('jane@example.com');
    expect(result.current.form.values.phone).toBe('+1234567890');
  });

  it('address fields remain empty when pre-filling from user profile', () => {
    const { result } = renderHookWithProviders(() => useCheckoutForm({ onSubmit: mockOnSubmit }), {
      preloadedState: authenticatedState,
    });

    expect(result.current.form.values.streetLine1).toBe('');
    expect(result.current.form.values.city).toBe('');
    expect(result.current.form.values.country).toBe('');
  });

  it('restores saved draft over user profile data', () => {
    const draft = {
      firstName: 'Old',
      lastName: 'Draft',
      email: 'old@draft.com',
      phone: '+9999999999',
      streetLine1: '1 Draft St',
      city: 'Draftville',
      state: 'DR',
      postalCode: '00000',
      country: 'GB',
    };
    vi.mocked(localStorageHook.useLocalStorage).mockReturnValue([draft, vi.fn()] as never);

    const { result } = renderHookWithProviders(() => useCheckoutForm({ onSubmit: mockOnSubmit }), {
      preloadedState: authenticatedState,
    });

    // Draft wins over user profile
    expect(result.current.form.values.firstName).toBe('Old');
    expect(result.current.form.values.email).toBe('old@draft.com');
    expect(result.current.form.values.country).toBe('GB');
  });

  it('restores saved draft for guest user', () => {
    const draft = { firstName: 'Guest', email: 'guest@example.com' };
    vi.mocked(localStorageHook.useLocalStorage).mockReturnValue([draft, vi.fn()] as never);

    const { result } = renderHookWithProviders(() => useCheckoutForm({ onSubmit: mockOnSubmit }), {
      preloadedState: guestState,
    });

    expect(result.current.form.values.firstName).toBe('Guest');
    expect(result.current.form.values.email).toBe('guest@example.com');
  });

  // ── Form API ────────────────────────────────────────────────────────────

  it('exposes form with values, errors, handleChange, and handleSubmit', () => {
    const { result } = renderHookWithProviders(() => useCheckoutForm({ onSubmit: mockOnSubmit }), {
      preloadedState: guestState,
    });

    expect(result.current.form.values).toBeDefined();
    expect(result.current.form.errors).toBeDefined();
    expect(result.current.form.handleChange).toBeTypeOf('function');
    expect(result.current.form.handleSubmit).toBeTypeOf('function');
    expect(result.current.form.isSubmitting).toBe(false);
  });

  // ── Draft persistence ───────────────────────────────────────────────────

  it('calls setShippingDraft after debounce when form values change', async () => {
    vi.useFakeTimers();
    const setDraft = vi.fn();
    vi.mocked(localStorageHook.useLocalStorage).mockReturnValue([{}, setDraft] as never);

    const { result } = renderHookWithProviders(() => useCheckoutForm({ onSubmit: mockOnSubmit }), {
      preloadedState: guestState,
    });

    // Trigger a re-render with new values to fire the effect
    result.current.form.setValues({ ...result.current.form.values, firstName: 'Test' });

    // Should not have written yet
    expect(setDraft).not.toHaveBeenCalled();

    // Advance past debounce
    vi.advanceTimersByTime(500);
    expect(setDraft).toHaveBeenCalled();

    vi.useRealTimers();
  });
});
