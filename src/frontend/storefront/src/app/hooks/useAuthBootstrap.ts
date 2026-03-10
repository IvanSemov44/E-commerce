import { useEffect } from 'react';
import { useAppDispatch, useAppSelector } from '@/shared/lib/store';
import { logger } from '@/shared/lib/utils/logger';
import { useErrorHandler } from '@/shared/hooks/useErrorHandler';
import { useGetCurrentUserQuery } from '@/features/auth/api/authApi';
import {
  selectAuthInitialized,
  selectCurrentUser,
  setInitialized,
  setUser,
} from '@/features/auth/slices/authSlice';

interface UseAuthBootstrapResult {
  initialized: boolean;
  isCurrentUserLoading: boolean;
}

export function useAuthBootstrap(): UseAuthBootstrapResult {
  const dispatch = useAppDispatch();
  const initialized = useAppSelector(selectAuthInitialized);
  const user = useAppSelector(selectCurrentUser);
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
          logger.error('useAuthBootstrap', 'Failed to fetch current user', currentUserError);
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

  return {
    initialized,
    isCurrentUserLoading,
  };
}
