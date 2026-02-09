/**
 * useAuth Hook
 * Centralized authentication logic (login, logout, registration, token management)
 */

import { useAppDispatch, useAppSelector } from '../store/hooks';
import {
  loginSuccess,
  loginFailure,
  logout,
  setUser,
} from '../store/slices/authSlice';
import type { AuthUser } from '../types';
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
  const persistToken = (authToken: string | null) => {
    if (authToken) {
      setStoredToken(authToken);
    } else {
      setStoredToken(null);
    }
  };

  /**
   * Handle successful login
   */
  const handleLoginSuccess = (userData: AuthUser, authToken: string) => {
    dispatch(loginSuccess({ user: userData, token: authToken }));
    persistToken(authToken);
    clearError();
  };

  /**
   * Handle login failure
   */
  const handleLoginFailure = (error: unknown) => {
    const errorState = handleError(error);
    dispatch(loginFailure(errorState.message));
  };

  /**
   * Update user profile in state
   */
  const updateProfile = (userData: Partial<AuthUser>) => {
    if (user) {
      dispatch(setUser({ ...user, ...userData }));
    }
  };

  /**
   * Logout and clear auth state
   */
  const performLogout = () => {
    dispatch(logout());
    persistToken(null);
    clearError();
  };

  /**
   * Check if user has specific role
   */
  const hasRole = (role: string | string[]): boolean => {
    if (!user) return false;
    const roles = Array.isArray(role) ? role : [role];
    return roles.includes(user.role);
  };

  /**
   * Check if token is valid (exists and decoded)
   */
  const isTokenValid = (): boolean => {
    return !!token && !!user;
  };

  /**
   * Get auth header for API calls
   */
  const getAuthHeader = (): Record<string, string> => {
    if (!token) return {};
    return {
      Authorization: `Bearer ${token}`,
    };
  };

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
