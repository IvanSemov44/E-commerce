import { renderHook, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import {
  selectAuthInitialized,
  selectCurrentUser,
  setInitialized,
  setUser,
} from '@/features/auth/slices/authSlice';
import { useAuthBootstrap } from './useAuthBootstrap';

const {
  selectorState,
  dispatchMock,
  queryHookMock,
  handleErrorMock,
  clearErrorMock,
  loggerErrorMock,
} = vi.hoisted(() => ({
  selectorState: {
    initialized: false,
    user: null,
  },
  dispatchMock: vi.fn(),
  queryHookMock: vi.fn(),
  handleErrorMock: vi.fn(),
  clearErrorMock: vi.fn(),
  loggerErrorMock: vi.fn(),
}));

vi.mock('@/shared/lib/store', () => ({
  useAppDispatch: () => dispatchMock,
  useAppSelector: (selector: unknown) => {
    if (selector === selectAuthInitialized) return selectorState.initialized;
    if (selector === selectCurrentUser) return selectorState.user;
    return undefined;
  },
}));

vi.mock('@/shared/hooks/useErrorHandler', () => ({
  useErrorHandler: () => ({
    handleError: handleErrorMock,
    clearError: clearErrorMock,
  }),
}));

vi.mock('@/features/auth/api/authApi', () => ({
  useGetCurrentUserQuery: queryHookMock,
}));

vi.mock('@/shared/lib/utils/logger', () => ({
  logger: {
    error: loggerErrorMock,
  },
}));

function setCurrentUserQueryResult(overrides: Record<string, unknown> = {}) {
  queryHookMock.mockReturnValue({
    data: null,
    isLoading: false,
    isSuccess: false,
    isError: false,
    error: undefined,
    ...overrides,
  });
}

describe('useAuthBootstrap', () => {
  beforeEach(() => {
    vi.clearAllMocks();

    selectorState.initialized = false;
    selectorState.user = null;

    setCurrentUserQueryResult();
  });

  it('hydrates user and normalizes missing role to customer', async () => {
    setCurrentUserQueryResult({
      data: {
        id: 'u1',
        email: 'user@test.com',
        firstName: 'Test',
        lastName: 'User',
      },
      isSuccess: true,
    });

    renderHook(() => useAuthBootstrap());

    await waitFor(() => {
      expect(dispatchMock).toHaveBeenCalledWith(
        setUser({
          id: 'u1',
          email: 'user@test.com',
          firstName: 'Test',
          lastName: 'User',
          role: 'customer',
        })
      );
    });

    expect(clearErrorMock).toHaveBeenCalledTimes(1);
    expect(dispatchMock).toHaveBeenCalledWith(setInitialized());
  });

  it('marks app initialized after successful current user fetch', async () => {
    setCurrentUserQueryResult({
      isSuccess: true,
    });

    renderHook(() => useAuthBootstrap());

    await waitFor(() => {
      expect(dispatchMock).toHaveBeenCalledWith(setInitialized());
    });
    expect(handleErrorMock).not.toHaveBeenCalled();
  });

  it('handles non-401 user fetch errors and still initializes app', async () => {
    const apiError = { status: 500, data: { message: 'server error' } };
    setCurrentUserQueryResult({
      isError: true,
      error: apiError,
    });

    renderHook(() => useAuthBootstrap());

    await waitFor(() => {
      expect(loggerErrorMock).toHaveBeenCalledWith(
        'useAuthBootstrap',
        'Failed to fetch current user',
        apiError
      );
      expect(handleErrorMock).toHaveBeenCalledWith(apiError);
      expect(dispatchMock).toHaveBeenCalledWith(setInitialized());
    });
  });

  it('ignores 401 errors while still setting initialized', async () => {
    const unauthorizedError = { status: 401 };
    setCurrentUserQueryResult({
      isError: true,
      error: unauthorizedError,
    });

    renderHook(() => useAuthBootstrap());

    await waitFor(() => {
      expect(dispatchMock).toHaveBeenCalledWith(setInitialized());
    });
    expect(handleErrorMock).not.toHaveBeenCalled();
    expect(loggerErrorMock).not.toHaveBeenCalled();
  });

  it('skips current-user query when already initialized', () => {
    selectorState.initialized = true;

    renderHook(() => useAuthBootstrap());

    expect(queryHookMock).toHaveBeenCalledWith(undefined, {
      skip: true,
    });
  });
});
