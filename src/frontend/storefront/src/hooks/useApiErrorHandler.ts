/**
 * useApiErrorHandler Hook
 * Centralized error handling for API requests
 * Extracts error messages and displays consistent notifications
 */

import { useToast } from './useToast';
import type { FetchBaseQueryError } from '@reduxjs/toolkit/query';
import type { SerializedError } from '@reduxjs/toolkit';

interface ErrorResponse {
  success: false;
  message?: string;
  errors?: Record<string, string[]>;
}

type ApiError = FetchBaseQueryError | SerializedError | Error | unknown;

interface UseApiErrorHandlerReturn {
  handleError: (error: ApiError, defaultMessage?: string) => void;
  getErrorMessage: (error: ApiError, defaultMessage?: string) => string;
}

export function useApiErrorHandler(): UseApiErrorHandlerReturn {
  const { toast } = useToast();

  const getErrorMessage = (error: ApiError, defaultMessage = 'An error occurred'): string => {
    // Handle RTK Query FetchBaseQueryError
    if (
      typeof error === 'object' &&
      error !== null &&
      'status' in error &&
      'data' in error
    ) {
      const apiError = error as any;

      // API response with message
      if (apiError.data?.message) {
        return apiError.data.message;
      }

      // Validation errors
      if (apiError.data?.errors && typeof apiError.data.errors === 'object') {
        const errorMessages = Object.values(apiError.data.errors).flat();
        if (errorMessages.length > 0) {
          return (errorMessages[0] as string) || defaultMessage;
        }
      }

      // HTTP status-based messages
      switch (apiError.status) {
        case 400:
          return 'Bad request. Please check your input.';
        case 401:
          return 'Unauthorized. Please log in.';
        case 403:
          return 'Forbidden. You do not have permission.';
        case 404:
          return 'The requested resource was not found.';
        case 409:
          return 'Conflict. The resource may have been modified.';
        case 500:
          return 'Server error. Please try again later.';
        case 503:
          return 'Service unavailable. Please try again later.';
        default:
          return defaultMessage;
      }
    }

    // Handle SerializedError
    if (typeof error === 'object' && error !== null && 'message' in error) {
      const serializedError = error as SerializedError;
      return serializedError.message || defaultMessage;
    }

    // Handle standard Error
    if (error instanceof Error) {
      return error.message || defaultMessage;
    }

    // Fallback
    return defaultMessage;
  };

  const handleError = (error: ApiError, defaultMessage?: string): void => {
    const message = getErrorMessage(error, defaultMessage);
    toast.error(message);
  };

  return {
    handleError,
    getErrorMessage,
  };
}
