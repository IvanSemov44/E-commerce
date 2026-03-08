import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { act } from '@testing-library/react';
import { renderHookWithProviders } from '@/shared/lib/test/test-utils';
import { baseApi } from '@/shared/lib/api/baseApi';
import { useProfileForm } from '../useProfileForm';

// Mock react-hot-toast
vi.mock('react-hot-toast', () => ({
  default: {
    error: vi.fn(),
    success: vi.fn(),
  },
  toast: {
    error: vi.fn(),
    success: vi.fn(),
  },
}));

// Mock API
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
  useChangePasswordMutation: vi.fn(() => [
    vi.fn().mockResolvedValue({ data: {} }),
    { isLoading: false },
  ]),
}));

describe('useProfileForm', () => {
  let store: ReturnType<typeof renderHookWithProviders>['store'];

  afterEach(() => {
    store?.dispatch(baseApi.util.resetApiState());
  });

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

  it('should initialize with formData', () => {
    const rendered = renderHookWithProviders(() => useProfileForm(), {
      preloadedState: defaultPreloadedState,
    });
    store = rendered.store;

    expect(rendered.result.current.formData.firstName).toBeDefined();
    expect(rendered.result.current.isEditMode).toBe(false);
    expect(rendered.result.current.isLoading).toBe(false);
  });

  it('should have handleSubmit function', () => {
    const rendered = renderHookWithProviders(() => useProfileForm(), {
      preloadedState: defaultPreloadedState,
    });
    store = rendered.store;

    expect(typeof rendered.result.current.handleSubmit).toBe('function');
  });

  it('should have handleCancel function', () => {
    const rendered = renderHookWithProviders(() => useProfileForm(), {
      preloadedState: defaultPreloadedState,
    });
    store = rendered.store;

    expect(typeof rendered.result.current.handleCancel).toBe('function');
  });

  it('should set edit mode', () => {
    const rendered = renderHookWithProviders(() => useProfileForm(), {
      preloadedState: defaultPreloadedState,
    });
    store = rendered.store;

    act(() => {
      rendered.result.current.setIsEditMode(true);
    });

    expect(rendered.result.current.isEditMode).toBe(true);
  });

  it('should set form data', () => {
    const rendered = renderHookWithProviders(() => useProfileForm(), {
      preloadedState: defaultPreloadedState,
    });
    store = rendered.store;

    act(() => {
      rendered.result.current.setFormData({
        firstName: 'Jane',
        lastName: 'Smith',
        email: 'jane@example.com',
        phone: '9876543210',
        avatarUrl: '',
      });
    });

    expect(rendered.result.current.formData.firstName).toBe('Jane');
    expect(rendered.result.current.formData.lastName).toBe('Smith');
  });
});
