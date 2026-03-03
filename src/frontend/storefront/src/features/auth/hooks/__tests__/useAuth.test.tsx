import { describe, it, expect, vi, beforeEach } from 'vitest';
import { renderHook, act } from '@testing-library/react';
import { configureStore } from '@reduxjs/toolkit';
import { Provider } from 'react-redux';
import { useAuth } from '@/features/auth/hooks/useAuth';
import { authSlice } from '@/features/auth/slices/authSlice';
import type { AuthUser } from '@/shared/types';

// Mock useErrorHandler
vi.mock('../useErrorHandler', () => ({
  useErrorHandler: () => ({
    handleError: vi.fn((error) => ({ message: String(error) })),
    clearError: vi.fn(),
  }),
}));

const createTestStore = (initialAuthState: Partial<{
  isAuthenticated: boolean;
  user: AuthUser | null;
  loading: boolean;
  error: string | null;
  initialized: boolean;
}> = {}) => {
  return configureStore({
    reducer: {
      auth: authSlice.reducer,
    },
    preloadedState: {
      auth: {
        isAuthenticated: false,
        user: null,
        loading: false,
        error: null,
        initialized: true,
        ...initialAuthState,
      },
    },
  });
};

const wrapper = (store: ReturnType<typeof createTestStore>) => {
  return function Wrapper({ children }: { children: React.ReactNode }) {
    return <Provider store={store}>{children}</Provider>;
  };
};

describe('useAuth', () => {
  const mockUser: AuthUser = {
    id: '1',
    email: 'test@example.com',
    firstName: 'John',
    lastName: 'Doe',
    role: 'Customer',
  };

  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('should return initial auth state when not authenticated', () => {
    const store = createTestStore();
    const { result } = renderHook(() => useAuth(), {
      wrapper: wrapper(store),
    });

    expect(result.current.isAuthenticated).toBe(false);
    expect(result.current.user).toBeNull();
    expect(result.current.loading).toBe(false);
  });

  it('should return authenticated state when user is logged in', () => {
    const store = createTestStore({
      isAuthenticated: true,
      user: mockUser,
    });
    const { result } = renderHook(() => useAuth(), {
      wrapper: wrapper(store),
    });

    expect(result.current.isAuthenticated).toBe(true);
    expect(result.current.user).toEqual(mockUser);
  });

  it('should handle login success', () => {
    const store = createTestStore();
    const { result } = renderHook(() => useAuth(), {
      wrapper: wrapper(store),
    });

    act(() => {
      result.current.handleLoginSuccess(mockUser);
    });

    expect(store.getState().auth.isAuthenticated).toBe(true);
    expect(store.getState().auth.user).toEqual(mockUser);
  });

  it('should handle login failure', () => {
    const store = createTestStore();
    const { result } = renderHook(() => useAuth(), {
      wrapper: wrapper(store),
    });

    act(() => {
      result.current.handleLoginFailure('Invalid credentials');
    });

    expect(store.getState().auth.isAuthenticated).toBe(false);
    expect(store.getState().auth.error).toBe('Invalid credentials');
  });

  it('should perform logout', () => {
    const store = createTestStore({
      isAuthenticated: true,
      user: mockUser,
    });
    const { result } = renderHook(() => useAuth(), {
      wrapper: wrapper(store),
    });

    act(() => {
      result.current.performLogout();
    });

    expect(store.getState().auth.isAuthenticated).toBe(false);
    expect(store.getState().auth.user).toBeNull();
  });

  it('should update profile', () => {
    const store = createTestStore({
      isAuthenticated: true,
      user: mockUser,
    });
    const { result } = renderHook(() => useAuth(), {
      wrapper: wrapper(store),
    });

    act(() => {
      result.current.updateProfile({ firstName: 'Jane' });
    });

    expect(store.getState().auth.user?.firstName).toBe('Jane');
    expect(store.getState().auth.user?.lastName).toBe('Doe');
  });

  it('should not update profile when user is null', () => {
    const store = createTestStore();
    const { result } = renderHook(() => useAuth(), {
      wrapper: wrapper(store),
    });

    act(() => {
      result.current.updateProfile({ firstName: 'Jane' });
    });

    expect(store.getState().auth.user).toBeNull();
  });

  it('should check if user has role (single role)', () => {
    const store = createTestStore({
      isAuthenticated: true,
      user: mockUser,
    });
    const { result } = renderHook(() => useAuth(), {
      wrapper: wrapper(store),
    });

    expect(result.current.hasRole('Customer')).toBe(true);
    expect(result.current.hasRole('Admin')).toBe(false);
  });

  it('should check if user has role (multiple roles)', () => {
    const store = createTestStore({
      isAuthenticated: true,
      user: mockUser,
    });
    const { result } = renderHook(() => useAuth(), {
      wrapper: wrapper(store),
    });

    expect(result.current.hasRole(['Admin', 'Customer'])).toBe(true);
    expect(result.current.hasRole(['Admin', 'Manager'])).toBe(false);
  });

  it('should return false for hasRole when user is null', () => {
    const store = createTestStore();
    const { result } = renderHook(() => useAuth(), {
      wrapper: wrapper(store),
    });

    expect(result.current.hasRole('Customer')).toBe(false);
  });

  it('should check if token is valid', () => {
    const store = createTestStore({
      isAuthenticated: true,
      user: mockUser,
    });
    const { result } = renderHook(() => useAuth(), {
      wrapper: wrapper(store),
    });

    expect(result.current.isTokenValid()).toBe(true);
  });

  it('should return false for isTokenValid when user is null', () => {
    const store = createTestStore();
    const { result } = renderHook(() => useAuth(), {
      wrapper: wrapper(store),
    });

    expect(result.current.isTokenValid()).toBe(false);
  });

  it('should get CSRF token from cookie', () => {
    document.cookie = 'XSRF-TOKEN=test-csrf-token; path=/';
    
    const store = createTestStore();
    const { result } = renderHook(() => useAuth(), {
      wrapper: wrapper(store),
    });

    expect(result.current.getCsrfToken()).toBe('test-csrf-token');
    
    // Cleanup
    document.cookie = 'XSRF-TOKEN=; expires=Thu, 01 Jan 1970 00:00:00 UTC; path=/';
  });

  it('should return null for CSRF token when not present', () => {
    document.cookie = 'XSRF-TOKEN=; expires=Thu, 01 Jan 1970 00:00:00 UTC; path=/';
    
    const store = createTestStore();
    const { result } = renderHook(() => useAuth(), {
      wrapper: wrapper(store),
    });

    expect(result.current.getCsrfToken()).toBeNull();
  });
});