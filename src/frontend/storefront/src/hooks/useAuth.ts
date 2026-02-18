/**
 * useAuth Hook
 * Centralized authentication logic (login, logout, registration)
 * Uses httpOnly cookies for secure token storage - no localStorage needed
 */

import { useAppDispatch, useAppSelector } from '../store/hooks';
import {
  loginSuccess,
  loginFailure,
  logout,
  setUser,
} from '../store/slices/authSlice';
import type { AuthUser } from '../types';
import { useErrorHandler } from './useErrorHandler';

export function useAuth() {
  const dispatch = useAppDispatch();
  const { isAuthenticated, user, loading } = useAppSelector(
    (state) => state.auth
  );
  const { handleError, clearError } = useErrorHandler();

  /**
   * Handle successful login
   * Tokens are now stored in httpOnly cookies by the backend
   */
  const handleLoginSuccess = (userData: AuthUser) => {
    dispatch(loginSuccess(userData));
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
   * Backend will clear httpOnly cookies
   */
  const performLogout = () => {
    dispatch(logout());
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
   * Check if user is authenticated
   * With httpOnly cookies, we rely on the user object being present
   */
  const isTokenValid = (): boolean => {
    return !!user;
  };

  /**
   * Get CSRF token from cookie for API calls
   */
  const getCsrfToken = (): string | null => {
    if (typeof document === 'undefined') return null;
    const match = document.cookie.match(/XSRF-TOKEN=([^;]+)/);
    return match ? decodeURIComponent(match[1]) : null;
  };

  return {
    // State
    isAuthenticated,
    user,
    loading,

    // Actions
    handleLoginSuccess,
    handleLoginFailure,
    updateProfile,
    performLogout,

    // Checks
    hasRole,
    isTokenValid,
    getCsrfToken,
  };
}
