/**
 * useToast Hook - Easy access to toast notifications
 * Provides methods: success, error, warning, info, clear
 */

import { useCallback } from 'react';
import { useAppDispatch } from '@/shared/lib/store';
import { addToast, removeToast, clearToasts as clearAllToasts } from '@/app/Toast/toastSlice';
import type { ToastVariant } from '@/app/Toast/toastSlice';
import { config } from '@/config';

interface UseToastReturn {
  success: (message: string, duration?: number) => string;
  error: (message: string, duration?: number) => string;
  warning: (message: string, duration?: number) => string;
  info: (message: string, duration?: number) => string;
  clear: (id: string) => void;
  clearAll: () => void;
  // Backward compatibility: allows destructuring { toast } for legacy code
  toast: {
    success: (message: string, duration?: number) => string;
    error: (message: string, duration?: number) => string;
    warning: (message: string, duration?: number) => string;
    info: (message: string, duration?: number) => string;
    clear: (id: string) => void;
    clearAll: () => void;
  };
}

export function useToast(): UseToastReturn {
  const dispatch = useAppDispatch();

  const showToast = useCallback(
    (message: string, variant: ToastVariant, duration?: number): string => {
      const id = `toast-${Date.now()}-${Math.random()}`;
      const toastDuration = duration ?? config.ui.toastDuration;

      dispatch(
        addToast({
          id,
          message,
          variant,
          duration: toastDuration,
        })
      );

      // Auto-remove after duration
      if (toastDuration > 0) {
        setTimeout(() => {
          dispatch(removeToast(id));
        }, toastDuration);
      }

      return id;
    },
    [dispatch]
  );

  const successFn = (message: string, duration?: number) => showToast(message, 'success', duration);
  const errorFn = (message: string, duration?: number) => showToast(message, 'error', duration);
  const warningFn = (message: string, duration?: number) => showToast(message, 'warning', duration);
  const infoFn = (message: string, duration?: number) => showToast(message, 'info', duration);
  const clearFn = (id: string) => dispatch(removeToast(id));
  const clearAllFn = () => dispatch(clearAllToasts());

  return {
    success: successFn,
    error: errorFn,
    warning: warningFn,
    info: infoFn,
    clear: clearFn,
    clearAll: clearAllFn,
    // Backward compatibility: allows { toast } = useToast(); toast.success()
    toast: {
      success: successFn,
      error: errorFn,
      warning: warningFn,
      info: infoFn,
      clear: clearFn,
      clearAll: clearAllFn,
    },
  };
}
