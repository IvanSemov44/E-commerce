import { useAppDispatch, useAppSelector } from '@/shared/lib/store';
import { loginSuccess, loginFailure, logout, setUser } from '../slices/authSlice';
import type { AuthUser } from '@/shared/types';
import { useErrorHandler } from '@/shared/hooks/useErrorHandler';

export function useAuth() {
  const dispatch = useAppDispatch();
  const { isAuthenticated, user, loading } = useAppSelector((state) => state.auth);
  const { handleError, clearError } = useErrorHandler();

  const handleLoginSuccess = (userData: AuthUser) => {
    dispatch(loginSuccess(userData));
    clearError();
  };

  const handleLoginFailure = (error: unknown) => {
    const errorState = handleError(error);
    dispatch(loginFailure(errorState.message));
  };

  const updateProfile = (userData: Partial<AuthUser>) => {
    if (user) {
      dispatch(setUser({ ...user, ...userData }));
    }
  };

  const performLogout = () => {
    dispatch(logout());
    clearError();
  };

  const hasRole = (role: string | string[]): boolean => {
    if (!user) return false;
    const roles = Array.isArray(role) ? role : [role];
    return roles.includes(user.role);
  };

  return {
    isAuthenticated,
    user,
    loading,
    handleLoginSuccess,
    handleLoginFailure,
    updateProfile,
    performLogout,
    hasRole,
  };
}
