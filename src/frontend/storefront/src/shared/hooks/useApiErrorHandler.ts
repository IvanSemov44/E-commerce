/**
 * useApiErrorHandler Hook
 * Centralized error handling for API requests
 * Extracts error messages and displays consistent notifications
 */

import { useToast } from '@/shared/components/Toast';
import type { FetchBaseQueryError } from '@reduxjs/toolkit/query';
import type { SerializedError } from '@reduxjs/toolkit';

type ApiError = FetchBaseQueryError | SerializedError | Error | unknown;

interface UseApiErrorHandlerReturn {
  handleError: (error: ApiError, defaultMessage?: string) => void;
  getErrorMessage: (error: ApiError, defaultMessage?: string) => string;
}

type RichFetchError = FetchBaseQueryError & {
  data?: { message?: string; errors?: Record<string, unknown> };
};

function getStatusMessage(status: number | string): string | null {
  switch (status) {
    case 400: return 'Bad request. Please check your input.';
    case 401: return 'Unauthorized. Please log in.';
    case 403: return 'Forbidden. You do not have permission.';
    case 404: return 'Resource not found.';
    case 409: return 'Conflict. The resource may have been modified.';
    case 500: return 'Server error. Please try again later.';
    case 503: return 'Service unavailable. Please try again later.';
    default:  return null;
  }
}

function getFetchErrorMessage(apiError: RichFetchError, defaultMessage: string): string {
  if (apiError.data?.message) return apiError.data.message;

  if (apiError.data?.errors && typeof apiError.data.errors === 'object') {
    const first = Object.values(apiError.data.errors).flat()[0];
    if (first) return first as string;
  }

  return getStatusMessage(apiError.status) ?? defaultMessage;
}

export function useApiErrorHandler(): UseApiErrorHandlerReturn {
  const { error: errorToast } = useToast();

  const getErrorMessage = (error: ApiError, defaultMessage = 'An error occurred'): string => {
    if (typeof error === 'object' && error !== null && 'status' in error) {
      return getFetchErrorMessage(error as RichFetchError, defaultMessage);
    }

    if (typeof error === 'object' && error !== null && 'message' in error) {
      return (error as SerializedError).message || defaultMessage;
    }

    if (error instanceof Error) return error.message || defaultMessage;

    return defaultMessage;
  };

  const handleError = (error: ApiError, defaultMessage?: string): void => {
    const message = getErrorMessage(error, defaultMessage);
    errorToast(message);
  };

  return {
    handleError,
    getErrorMessage,
  };
}
