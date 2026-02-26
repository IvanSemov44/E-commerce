import { describe, it, expect, vi } from 'vitest';
import { renderHook, act } from '@testing-library/react';
import { useErrorHandler } from '../useErrorHandler';

describe('useErrorHandler', () => {
  it('returns initial state with no error', () => {
    const { result } = renderHook(() => useErrorHandler());
    
    expect(result.current.error).toBeNull();
    expect(result.current.isLoading).toBe(false);
    expect(result.current.hasError).toBe(false);
  });

  it('handles Error instance', () => {
    const { result } = renderHook(() => useErrorHandler());
    
    act(() => {
      result.current.handleError(new Error('Test error message'));
    });
    
    expect(result.current.error).toEqual({
      message: 'Test error message',
    });
    expect(result.current.hasError).toBe(true);
  });

  it('handles RTK Query error structure', () => {
    const { result } = renderHook(() => useErrorHandler());
    
    const rtkError = {
      data: {
        message: 'RTK Error message',
        errors: { field1: 'Error 1' },
      },
      status: 400,
    };
    
    act(() => {
      result.current.handleError(rtkError);
    });
    
    expect(result.current.error?.message).toBe('RTK Error message');
    expect(result.current.error?.status).toBe(400);
    expect(result.current.error?.fieldErrors).toEqual({ field1: 'Error 1' });
  });

  it('handles plain object with message property', () => {
    const { result } = renderHook(() => useErrorHandler());
    
    const apiError = {
      message: 'API Error',
      status: 500,
    };
    
    act(() => {
      result.current.handleError(apiError);
    });
    
    expect(result.current.error?.message).toBe('API Error');
    expect(result.current.error?.status).toBe(500);
  });

  it('handles unknown error', () => {
    const { result } = renderHook(() => useErrorHandler());
    
    act(() => {
      result.current.handleError('string error');
    });
    
    expect(result.current.error?.message).toBe('An unknown error occurred');
  });

  it('clears error', () => {
    const { result } = renderHook(() => useErrorHandler());
    
    act(() => {
      result.current.handleError(new Error('Test error'));
    });
    
    expect(result.current.hasError).toBe(true);
    
    act(() => {
      result.current.clearError();
    });
    
    expect(result.current.error).toBeNull();
    expect(result.current.hasError).toBe(false);
  });

  it('sets isLoading correctly', () => {
    const { result } = renderHook(() => useErrorHandler());
    
    expect(result.current.isLoading).toBe(false);
    
    act(() => {
      result.current.setIsLoading(true);
    });
    
    expect(result.current.isLoading).toBe(true);
  });

  it('detects client errors (4xx)', () => {
    const { result } = renderHook(() => useErrorHandler());
    
    act(() => {
      result.current.handleError({ message: 'Client error', status: 400 });
    });
    
    expect(result.current.isClientError()).toBe(true);
    expect(result.current.isServerError()).toBe(false);
  });

  it('detects server errors (5xx)', () => {
    const { result } = renderHook(() => useErrorHandler());
    
    act(() => {
      result.current.handleError({ message: 'Server error', status: 500 });
    });
    
    expect(result.current.isClientError()).toBe(false);
    expect(result.current.isServerError()).toBe(true);
  });

  it('returns false for isClientError when no error', () => {
    const { result } = renderHook(() => useErrorHandler());
    expect(result.current.isClientError()).toBe(false);
  });

  it('returns false for isServerError when no error', () => {
    const { result } = renderHook(() => useErrorHandler());
    expect(result.current.isServerError()).toBe(false);
  });
});
