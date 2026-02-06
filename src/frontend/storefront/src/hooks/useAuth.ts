/**
 * useAuth Hook
 * Centralized authentication logic (login, logout, registration, token management)
 */

import { useCallback } from 'react';
import { useAppDispatch, useAppSelector } from '../store/hooks';
import {
  loginSuccess,
  loginFailure,
  logout,
  setUser,
} from '../store/slices/authSlice';
import type { AuthUser, LoginRequest, RegisterRequest } from '../types';
import { config } from '../config';
import { useErrorHandler } from './useErrorHandler';
import { useLocalStorage } from './useLocalStorage';

export function useAuth() {
  const dispatch = useAppDispatch();
  const { isAuthenticated, user, token, loading } = useAppSelector(
    (state) => state.auth
  );
  const { handleError, clearError } = useErrorHandler();
  const [, setStoredToken] = useLocalStorage<string | null>(
    config.storage.authToken,
    null
  );

  /**
   * Persist token to localStorage when it changes
   */
  const persistToken = useCallback((authToken: string | null) => {
    if (authToken) {
      setStoredToken(authToken);
    } else {
      setStoredToken(null);
    }
  }, [setStoredToken]);

  /**
   * Handle successful login
   */
  const handleLoginSuccess = useCallback(
    (userData: AuthUser, authToken: string) => {
      dispatch(loginSuccess({ user: userData, token: authToken }));
      persistToken(authToken);
      clearError();
    },
    [dispatch, persistToken, clearError]
  );

  /**
   * Handle login failure
   */
  const handleLoginFailure = useCallback(
    (error: unknown) => {
      const errorState = handleError(error);
      dispatch(loginFailure(errorState.message));
    },
    [dispatch, handleError]
  );

  /**
   * Update user profile in state
   */
  const updateProfile = useCallback(
    (userData: Partial<AuthUser>) => {
      if (user) {
        dispatch(setUser({ ...user, ...userData }));
      }
    },
    [dispatch, user]
  );

  /**
   * Logout and clear auth state
   */
  const performLogout = useCallback(() => {
    dispatch(logout());
    persistToken(null);
    clearError();
  }, [dispatch, persistToken, clearError]);

  /**
   * Check if user has specific role
   */
  const hasRole = useCallback(
    (role: string | string[]): boolean => {
      if (!user) return false;
      const roles = Array.isArray(role) ? role : [role];
      return roles.includes(user.role);
    },
    [user]
  );

  /**
   * Check if token is valid (exists and decoded)
   */
  const isTokenValid = useCallback((): boolean => {
    return !!token && !!user;
  }, [token, user]);

  /**
   * Get auth header for API calls
   */
  const getAuthHeader = useCallback((): Record<string, string> => {
    if (!token) return {};
    return {
      Authorization: `Bearer ${token}`,
    };
  }, [token]);

  return {
    // State
    isAuthenticated,
    user,
    token,
    loading,

    // Actions
    handleLoginSuccess,
    handleLoginFailure,
    updateProfile,
    performLogout,

    // Checks
    hasRole,
    isTokenValid,
    getAuthHeader,
  };
}
