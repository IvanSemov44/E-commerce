import { renderHook } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { selectIsAuthenticated } from '@/features/auth/slices/authSlice';
import { useAppInitialization } from './useAppInitialization';

const { selectorState, authBootstrapMock, cartSyncMock } = vi.hoisted(() => ({
  selectorState: {
    isAuthenticated: false,
  },
  authBootstrapMock: vi.fn(),
  cartSyncMock: vi.fn(),
}));

vi.mock('@/shared/lib/store', () => ({
  useAppSelector: (selector: unknown) => {
    if (selector === selectIsAuthenticated) return selectorState.isAuthenticated;
    return undefined;
  },
}));

vi.mock('./useAuthBootstrap', () => ({
  useAuthBootstrap: authBootstrapMock,
}));

vi.mock('@/features/cart/hooks', () => ({
  useCartSync: cartSyncMock,
}));

function setBootstrapState(initialized: boolean, isCurrentUserLoading = false) {
  authBootstrapMock.mockReturnValue({
    initialized,
    isCurrentUserLoading,
  });
}

function setCartSyncLoading(isLoading: boolean) {
  cartSyncMock.mockReturnValue({
    isLoading,
  });
}

describe('useAppInitialization', () => {
  beforeEach(() => {
    vi.clearAllMocks();

    selectorState.isAuthenticated = false;
    setBootstrapState(false, false);
    setCartSyncLoading(false);
  });

  it('returns initializing=true while auth bootstrap is not initialized', () => {
    const { result } = renderHook(() => useAppInitialization());

    expect(result.current.isInitializing).toBe(true);
  });

  it('returns initializing=true while current user query is loading', () => {
    setBootstrapState(true, true);

    const { result } = renderHook(() => useAppInitialization());

    expect(result.current.isInitializing).toBe(true);
  });

  it('returns initializing=true while cart sync is loading', () => {
    selectorState.isAuthenticated = true;
    setBootstrapState(true, false);
    setCartSyncLoading(true);

    const { result } = renderHook(() => useAppInitialization());

    expect(result.current.isInitializing).toBe(true);
  });

  it('returns initializing=false when all startup tasks are done', () => {
    selectorState.isAuthenticated = true;
    setBootstrapState(true, false);
    setCartSyncLoading(false);

    const { result } = renderHook(() => useAppInitialization());

    expect(result.current.isInitializing).toBe(false);
  });

  it('enables cart sync only when initialized and authenticated', () => {
    selectorState.isAuthenticated = true;
    setBootstrapState(true, false);

    renderHook(() => useAppInitialization());

    expect(cartSyncMock).toHaveBeenCalledWith({
      enabled: true,
    });
  });

  it('disables cart sync when unauthenticated', () => {
    selectorState.isAuthenticated = false;
    setBootstrapState(true, false);

    renderHook(() => useAppInitialization());

    expect(cartSyncMock).toHaveBeenCalledWith({
      enabled: false,
    });
  });
});
