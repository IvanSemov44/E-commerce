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

const defaultErrorState: ErrorState = {
  message: 'An unknown error occurred',
};

function normalizeRtkQueryError(err: {
  data?: { message?: string; errors?: unknown };
  status?: number;
}): ErrorState {
  const normalized: ErrorState = {
    message: err.data?.message ?? defaultErrorState.message,
    status: err.status,
  };

  if (typeof err.data?.errors === 'object' && err.data?.errors && !Array.isArray(err.data.errors)) {
    normalized.fieldErrors = err.data.errors as Record<string, string>;
  }

  if (Array.isArray(err.data?.errors)) {
    normalized.message = err.data.errors.join(', ');
  }

  return normalized;
}

function normalizeApiError(err: ApiError): ErrorState {
  return {
    message: err.message,
    status: err.status,
    fieldErrors: err.errors
      ? Object.fromEntries(
          Object.entries(err.errors).map(([key, val]) => [key, Array.isArray(val) ? val[0] : val])
        )
      : undefined,
  };
}

export function useErrorHandler() {
  const [error, setError] = useState<ErrorState | null>(null);
  const [isLoading, setIsLoading] = useState(false);

  /**
   * Handle API errors and normalize them
   */
  const handleError = useCallback((err: unknown): ErrorState => {
    let errorState = defaultErrorState;

    if (err && typeof err === 'object' && 'data' in err) {
      errorState = normalizeRtkQueryError(
        err as { data?: { message?: string; errors?: unknown }; status?: number }
      );
    } else if (err instanceof Error) {
      errorState = { message: err.message };
    } else if (err && typeof err === 'object' && 'message' in err) {
      errorState = normalizeApiError(err as ApiError);
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
  const getFieldError = useCallback(
    (fieldName: string): string | undefined => {
      return error?.fieldErrors?.[fieldName];
    },
    [error]
  );

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
