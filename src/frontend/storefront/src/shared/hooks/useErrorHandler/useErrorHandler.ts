/**
 * useErrorHandler Hook
 * Centralized error handling for API calls and user notifications
 */

import { useState, useCallback } from 'react';
import type { ApiError } from '@/shared/types';

export interface ErrorState {
  message: string;
  status?: number;
  fieldErrors?: Record<string, string>;
}

export function useErrorHandler() {
  const [error, setError] = useState<ErrorState | null>(null);
  const [isLoading, setIsLoading] = useState(false);

  /**
   * Handle API errors and normalize them
   */
  const handleError = useCallback((err: unknown): ErrorState => {
    let errorState: ErrorState = {
      message: 'An unknown error occurred',
    };

    // RTK Query error structure
    if (err && typeof err === 'object' && 'data' in err) {
      const rtqError = err as { data?: { message?: string; errors?: unknown }; status?: number };
      if (rtqError.data?.message) {
        errorState.message = rtqError.data.message;
      }
      if (rtqError.data?.errors) {
        if (typeof rtqError.data.errors === 'object' && !Array.isArray(rtqError.data.errors)) {
          errorState.fieldErrors = rtqError.data.errors as Record<string, string>;
        } else if (Array.isArray(rtqError.data.errors)) {
          errorState.message = rtqError.data.errors.join(', ');
        }
      }
      errorState.status = rtqError.status;
    }

    // Fetch error
    else if (err instanceof Error) {
      errorState.message = err.message;
    }

    // Custom ApiError
    else if (err && typeof err === 'object' && 'message' in err) {
      const apiErr = err as ApiError;
      errorState = {
        message: apiErr.message,
        status: apiErr.status,
        fieldErrors: apiErr.errors ? Object.fromEntries(
          Object.entries(apiErr.errors).map(([key, val]) => [
            key,
            Array.isArray(val) ? val[0] : val
          ])
        ) : undefined,
      };
    }

    setError(errorState);
    return errorState;
  }, []);

  /**
   * Clear error
   */
  const clearError = useCallback(() => {
    setError(null);
  }, []);

  /**
   * Get field-specific error
   */
  const getFieldError = useCallback((fieldName: string): string | undefined => {
    return error?.fieldErrors?.[fieldName];
  }, [error]);

  /**
   * Check if it's a client-side error (4xx)
   */
  const isClientError = useCallback((): boolean => {
    const status = error?.status;
    return !!(status && status >= 400 && status < 500);
  }, [error?.status]);

  /**
   * Check if it's a server error (5xx)
   */
  const isServerError = useCallback((): boolean => {
    const status = error?.status;
    return !!(status && status >= 500);
  }, [error?.status]);

  return {
    error,
    isLoading,
    setIsLoading,
    handleError,
    clearError,
    getFieldError,
    isClientError,
    isServerError,
    hasError: error !== null,
  };
}
