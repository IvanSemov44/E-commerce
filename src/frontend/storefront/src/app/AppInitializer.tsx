import { useEffect, type ReactNode } from 'react';
import { useLocation } from 'react-router-dom';
import { useAppDispatch, useAppSelector } from '@/shared/lib/store';
import { telemetry } from '@/shared/lib/utils/telemetry';
import { logger } from '@/shared/lib/utils/logger';
import { useErrorHandler } from '@/shared/hooks/useErrorHandler';
import { useCartSync } from '@/features/cart/hooks';
import { useGetCurrentUserQuery } from '@/features/auth/api/authApi';
import {
  selectAuthInitialized,
  selectCurrentUser,
  selectIsAuthenticated,
  setInitialized,
  setUser,
} from '@/features/auth/slices/authSlice';

interface AppInitializerProps {
  children: (state: { isInitializing: boolean }) => ReactNode;
}

export default function AppInitializer({ children }: AppInitializerProps) {
  const dispatch = useAppDispatch();
  const location = useLocation();
  const initialized = useAppSelector(selectAuthInitialized);
  const user = useAppSelector(selectCurrentUser);
  const isAuthenticated = useAppSelector(selectIsAuthenticated);
  const { handleError, clearError } = useErrorHandler();

  const {
    data: currentUser,
    isLoading: isCurrentUserLoading,
    isSuccess: isCurrentUserSuccess,
    isError: isCurrentUserError,
    error: currentUserError,
  } = useGetCurrentUserQuery(undefined, {
    skip: initialized,
  });

  const { isLoading: cartLoading } = useCartSync({
    enabled: initialized && isAuthenticated,
  });

  useEffect(() => {
    telemetry.track('route.change', { path: location.pathname });
  }, [location.pathname]);

  useEffect(() => {
    if (currentUser && !user) {
      dispatch(
        setUser({
          ...currentUser,
          role: currentUser.role || 'customer',
        })
      );
      clearError();
    }
  }, [currentUser, user, dispatch, clearError]);

  useEffect(() => {
    if (!initialized && (isCurrentUserSuccess || isCurrentUserError)) {
      if (isCurrentUserError && currentUserError) {
        const status = (currentUserError as { status?: number }).status;

        // 401 here means no active session and should not surface as a global app error.
        if (status !== 401) {
          logger.error('AppInitializer', 'Failed to fetch current user', currentUserError);
          handleError(currentUserError);
        }
      }

      dispatch(setInitialized());
    }
  }, [
    initialized,
    isCurrentUserSuccess,
    isCurrentUserError,
    currentUserError,
    handleError,
    dispatch,
  ]);

  const isInitializing = !initialized || isCurrentUserLoading || cartLoading;

  return <>{children({ isInitializing })}</>;
}
