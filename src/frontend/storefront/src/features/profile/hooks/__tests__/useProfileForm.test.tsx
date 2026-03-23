import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { act } from '@testing-library/react';
import { renderHookWithProviders } from '@/shared/lib/test/test-utils';
import { baseApi } from '@/shared/lib/api/baseApi';
import { useProfileForm } from '../useProfileForm';

vi.mock('../../api/profileApi', () => ({
  useGetProfileQuery: vi.fn(() => ({
    data: {
      id: '1',
      email: 'test@example.com',
      firstName: 'John',
      lastName: 'Doe',
      phone: '1234567890',
      avatarUrl: '',
    },
    isLoading: false,
  })),
  useUpdateProfileMutation: vi.fn(() => [
    vi.fn().mockResolvedValue({ data: {} }),
    { isLoading: false },
  ]),
}));

describe('useProfileForm', () => {
  let store: ReturnType<typeof renderHookWithProviders>['store'];

  const defaultPreloadedState = {
    auth: {
      isAuthenticated: true,
      user: {
        id: '1',
        email: 'test@example.com',
        firstName: 'John',
        lastName: 'Doe',
        phone: '1234567890',
        role: 'customer' as const,
      },
      loading: false,
      error: null,
      initialized: true,
    },
  };

  beforeEach(() => {
    vi.clearAllMocks();
  });

  afterEach(() => {
    store?.dispatch(baseApi.util.resetApiState());
  });

  it('initialises with profile data synced into values', () => {
    const rendered = renderHookWithProviders(() => useProfileForm(), {
      preloadedState: defaultPreloadedState,
    });
    store = rendered.store;

    expect(rendered.result.current.values.firstName).toBeDefined();
    expect(rendered.result.current.isEditMode).toBe(false);
    expect(rendered.result.current.isLoading).toBe(false);
  });

  it('exposes action and isPending for form submission', () => {
    const rendered = renderHookWithProviders(() => useProfileForm(), {
      preloadedState: defaultPreloadedState,
    });
    store = rendered.store;

    expect(typeof rendered.result.current.action).toBe('function');
    expect(typeof rendered.result.current.isPending).toBe('boolean');
  });

  it('exposes handleCancel', () => {
    const rendered = renderHookWithProviders(() => useProfileForm(), {
      preloadedState: defaultPreloadedState,
    });
    store = rendered.store;

    expect(typeof rendered.result.current.handleCancel).toBe('function');
  });

  it('toggles edit mode via setIsEditMode', () => {
    const rendered = renderHookWithProviders(() => useProfileForm(), {
      preloadedState: defaultPreloadedState,
    });
    store = rendered.store;

    act(() => {
      rendered.result.current.setIsEditMode(true);
    });

    expect(rendered.result.current.isEditMode).toBe(true);
  });

  it('handleCancel resets edit mode and clears field errors', () => {
    const rendered = renderHookWithProviders(() => useProfileForm(), {
      preloadedState: defaultPreloadedState,
    });
    store = rendered.store;

    act(() => {
      rendered.result.current.setIsEditMode(true);
    });

    act(() => {
      rendered.result.current.handleCancel();
    });

    expect(rendered.result.current.isEditMode).toBe(false);
    expect(rendered.result.current.fieldErrors).toEqual({});
  });
});
