/**
 * useToast Hook - Admin Panel
 * Easy access to toast notifications
 */

import { useCallback } from 'react';
import { useAppDispatch } from '../store/hooks';
import { addToast, removeToast, clearToasts, ToastVariant } from '../store/slices/toastSlice';
import config from '../config';

interface UseToastReturn {
  success: (message: string, duration?: number) => string;
  error: (message: string, duration?: number) => string;
  warning: (message: string, duration?: number) => string;
  info: (message: string, duration?: number) => string;
  clear: (id: string) => void;
  clearAll: () => void;
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

  return {
    success: (message: string, duration?: number) => showToast(message, 'success', duration),
    error: (message: string, duration?: number) => showToast(message, 'error', duration),
    warning: (message: string, duration?: number) => showToast(message, 'warning', duration),
    info: (message: string, duration?: number) => showToast(message, 'info', duration),
    clear: (id: string) => dispatch(removeToast(id)),
    clearAll: () => dispatch(clearToasts()),
  };
};
