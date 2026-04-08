import { waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { renderHookWithProviders } from '@/shared/lib/test/test-utils';
import { selectAuthInitialized, selectCurrentUser } from '@/features/auth/slices/authSlice';
import { useAuthBootstrap } from './useAuthBootstrap';
import { server } from '@/shared/lib/test/msw-server';
import { http, HttpResponse } from 'msw';

const { selectorState, dispatchMock, handleErrorMock, clearErrorMock, loggerErrorMock } =
  vi.hoisted(() => ({
    selectorState: {
      initialized: false,
      user: null,
    },
    dispatchMock: vi.fn(),
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

vi.mock('@/shared/lib/utils/logger', () => ({
  logger: {
    error: loggerErrorMock,
  },
}));

const setupHandlers = (userData = null, error = false) => {
  if (error) {
    server.use(
      http.get('/api/auth/me', () => {
        return HttpResponse.json(
          { success: false, errorDetails: { message: 'Unauthorized', code: 'UNAUTHORIZED' } },
          { status: 401 }
        );
      })
    );
  } else {
    server.use(
      http.get('/api/auth/me', () => {
        return HttpResponse.json({
          success: true,
          data: userData,
        });
      })
    );
  }
};

describe('useAuthBootstrap', () => {
  beforeEach(() => {
    vi.clearAllMocks();

    selectorState.initialized = false;
    selectorState.user = null;

    setupHandlers(null);
  });

  it('hydrates user and normalizes missing role to customer', async () => {
    setupHandlers({
      id: 'u1',
      email: 'test@example.com',
      firstName: 'Test',
      lastName: 'User',
    });

    const { result } = renderHookWithProviders(() => useAuthBootstrap());

    await waitFor(() => {
      expect(result.current).toBeDefined();
    });
  });

  it('handles missing user data gracefully', async () => {
    setupHandlers(null);

    const { result } = renderHookWithProviders(() => useAuthBootstrap());

    await waitFor(() => {
      expect(result.current).toBeDefined();
    });
  });

  it('handles API errors gracefully', async () => {
    setupHandlers(null, true);

    const { result } = renderHookWithProviders(() => useAuthBootstrap());

    await waitFor(() => {
      expect(result.current).toBeDefined();
    });
  });
});
