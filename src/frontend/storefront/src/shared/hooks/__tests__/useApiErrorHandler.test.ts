import { describe, it, expect, vi, beforeEach } from 'vitest';
import { renderHook } from '@testing-library/react';
import { useApiErrorHandler } from '../useApiErrorHandler';

// Mock useToast
vi.mock('../useToast', () => ({
  useToast: () => ({
    error: vi.fn(),
    success: vi.fn(),
    info: vi.fn(),
    warning: vi.fn(),
  }),
}));

describe('useApiErrorHandler', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('returns handleError and getErrorMessage functions', () => {
    const { result } = renderHook(() => useApiErrorHandler());
    
    expect(result.current.handleError).toBeDefined();
    expect(result.current.getErrorMessage).toBeDefined();
    expect(typeof result.current.handleError).toBe('function');
    expect(typeof result.current.getErrorMessage).toBe('function');
  });

  it('extracts error message from FetchBaseQueryError with message', () => {
    const { result } = renderHook(() => useApiErrorHandler());
    
    const error = {
      status: 400,
      data: {
        message: 'Bad request error',
      },
    };
    
    const message = result.current.getErrorMessage(error);
    expect(message).toBe('Bad request error');
  });

  it('returns default message for unknown error structure', () => {
    const { result } = renderHook(() => useApiErrorHandler());
    
    const message = result.current.getErrorMessage('unknown', 'Custom default');
    expect(message).toBe('Custom default');
  });

  it('handles 400 status with default message', () => {
    const { result } = renderHook(() => useApiErrorHandler());
    
    const error = { status: 400 };
    const message = result.current.getErrorMessage(error);
    expect(message).toBe('Bad request. Please check your input.');
  });

  it('handles 401 status with default message', () => {
    const { result } = renderHook(() => useApiErrorHandler());
    
    const error = { status: 401 };
    const message = result.current.getErrorMessage(error);
    expect(message).toBe('Unauthorized. Please log in.');
  });

  it('handles 403 status with default message', () => {
    const { result } = renderHook(() => useApiErrorHandler());
    
    const error = { status: 403 };
    const message = result.current.getErrorMessage(error);
    expect(message).toBe('Forbidden. You do not have permission.');
  });

  it('handles 404 status with default message', () => {
    const { result } = renderHook(() => useApiErrorHandler());
    
    const error = { status: 404 };
    const message = result.current.getErrorMessage(error);
    expect(message).toBe('Resource not found.');
  });

  it('handles 500 status with default message', () => {
    const { result } = renderHook(() => useApiErrorHandler());
    
    const error = { status: 500 };
    const message = result.current.getErrorMessage(error);
    expect(message).toBe('Server error. Please try again later.');
  });

  it('handles Error instance', () => {
    const { result } = renderHook(() => useApiErrorHandler());
    
    const error = new Error('Network error');
    const message = result.current.getErrorMessage(error);
    expect(message).toBe('Network error');
  });
});
