/**
 * useToast Hook - Admin Panel
 * Easy access to toast notifications
 */

import { useCallback } from 'react';
import { useAppDispatch } from '../store/hooks';
import { addToast, removeToast, clearToasts } from '../store/slices/toastSlice';
import type { ToastVariant } from '../store/slices/toastSlice';
import config from '../config';

interface UseToastReturn {
  success: (message: string, duration?: number) => string;
  error: (message: string, duration?: number) => string;
  warning: (message: string, duration?: number) => string;
  info: (message: string, duration?: number) => string;
  clear: (id: string) => void;
  clearAll: () => void;
  toast: {
    success: (message: string, duration?: number) => string;
    error: (message: string, duration?: number) => string;
    warning: (message: string, duration?: number) => string;
    info: (message: string, duration?: number) => string;
    clear: (id: string) => void;
    clearAll: () => void;
  };
}

export const useToast = (): UseToastReturn => {
  const dispatch = useAppDispatch();

  const showToast = useCallback(
    (message: string, variant: ToastVariant, duration?: number): string => {
      const id = `toast-${Date.now()}-${Math.random()}`;
      const toastDuration = duration || config.ui.toastDuration;

      dispatch(
        addToast({
          id,
          message,
          variant,
          duration: toastDuration,
        })
      );

      // Auto-remove after duration
      setTimeout(() => {
        dispatch(removeToast(id));
      }, toastDuration);

      return id;
    },
    [dispatch]
  );

  const success = (message: string, duration?: number) => showToast(message, 'success', duration);
  const error = (message: string, duration?: number) => showToast(message, 'error', duration);
  const warning = (message: string, duration?: number) => showToast(message, 'warning', duration);
  const info = (message: string, duration?: number) => showToast(message, 'info', duration);
  const clear = (id: string) => dispatch(removeToast(id));
  const clearAll = () => dispatch(clearToasts());

  return {
    success,
    error,
    warning,
    info,
    clear,
    clearAll,
    toast: {
      success,
      error,
      warning,
      info,
      clear,
      clearAll,
    },
  };
};
