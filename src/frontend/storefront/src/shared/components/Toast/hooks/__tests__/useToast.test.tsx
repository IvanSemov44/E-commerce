import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { renderHook, act } from '@testing-library/react';
import { Provider } from 'react-redux';
import { configureStore } from '@reduxjs/toolkit';
import { useToast } from '../useToast';
import { toastSlice } from '@/shared/components/Toast/toastSlice';
import type { ReactNode } from 'react';

// Create a wrapper with Redux store
const createWrapper = () => {
  const store = configureStore({
    reducer: {
      toast: toastSlice.reducer,
    },
  });

  const wrapper = ({ children }: { children: ReactNode }) => (
    <Provider store={store}>{children}</Provider>
  );

  return { store, wrapper };
};

describe('useToast', () => {
  beforeEach(() => {
    vi.useFakeTimers();
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  describe('success', () => {
    it('should dispatch a success toast', () => {
      const { wrapper, store } = createWrapper();
      const { result } = renderHook(() => useToast(), { wrapper });

      act(() => {
        result.current.success('Success message');
      });

      const state = store.getState().toast;
      expect(state.toasts).toHaveLength(1);
      expect(state.toasts[0].message).toBe('Success message');
      expect(state.toasts[0].variant).toBe('success');
    });

    it('should return the toast id', () => {
      const { wrapper } = createWrapper();
      const { result } = renderHook(() => useToast(), { wrapper });

      let toastId: string = '';
      act(() => {
        toastId = result.current.success('Success message');
      });

      expect(toastId).toMatch(/^toast-\d+-[\d.]+$/);
    });

    it('should use custom duration', () => {
      const { wrapper, store } = createWrapper();
      const { result } = renderHook(() => useToast(), { wrapper });

      act(() => {
        result.current.success('Success message', 10000);
      });

      const state = store.getState().toast;
      expect(state.toasts[0].duration).toBe(10000);
    });
  });

  describe('error', () => {
    it('should dispatch an error toast', () => {
      const { wrapper, store } = createWrapper();
      const { result } = renderHook(() => useToast(), { wrapper });

      act(() => {
        result.current.error('Error message');
      });

      const state = store.getState().toast;
      expect(state.toasts).toHaveLength(1);
      expect(state.toasts[0].message).toBe('Error message');
      expect(state.toasts[0].variant).toBe('error');
    });

    it('should return the toast id', () => {
      const { wrapper } = createWrapper();
      const { result } = renderHook(() => useToast(), { wrapper });

      let toastId: string = '';
      act(() => {
        toastId = result.current.error('Error message');
      });

      expect(toastId).toMatch(/^toast-\d+-[\d.]+$/);
    });
  });

  describe('warning', () => {
    it('should dispatch a warning toast', () => {
      const { wrapper, store } = createWrapper();
      const { result } = renderHook(() => useToast(), { wrapper });

      act(() => {
        result.current.warning('Warning message');
      });

      const state = store.getState().toast;
      expect(state.toasts).toHaveLength(1);
      expect(state.toasts[0].message).toBe('Warning message');
      expect(state.toasts[0].variant).toBe('warning');
    });
  });

  describe('info', () => {
    it('should dispatch an info toast', () => {
      const { wrapper, store } = createWrapper();
      const { result } = renderHook(() => useToast(), { wrapper });

      act(() => {
        result.current.info('Info message');
      });

      const state = store.getState().toast;
      expect(state.toasts).toHaveLength(1);
      expect(state.toasts[0].message).toBe('Info message');
      expect(state.toasts[0].variant).toBe('info');
    });
  });

  describe('clear', () => {
    it('should remove a specific toast', () => {
      const { wrapper, store } = createWrapper();
      const { result } = renderHook(() => useToast(), { wrapper });

      let toastId: string = '';
      act(() => {
        toastId = result.current.success('Test message');
      });

      expect(store.getState().toast.toasts).toHaveLength(1);

      act(() => {
        result.current.clear(toastId);
      });

      expect(store.getState().toast.toasts).toHaveLength(0);
    });
  });

  describe('clearAll', () => {
    it('should remove all toasts', () => {
      const { wrapper, store } = createWrapper();
      const { result } = renderHook(() => useToast(), { wrapper });

      act(() => {
        result.current.success('Success');
        result.current.error('Error');
        result.current.warning('Warning');
      });

      expect(store.getState().toast.toasts).toHaveLength(3);

      act(() => {
        result.current.clearAll();
      });

      expect(store.getState().toast.toasts).toHaveLength(0);
    });
  });

  describe('toast object (backward compatibility)', () => {
    it('should provide toast.success method', () => {
      const { wrapper, store } = createWrapper();
      const { result } = renderHook(() => useToast(), { wrapper });

      act(() => {
        result.current.toast.success('Success via toast object');
      });

      const state = store.getState().toast;
      expect(state.toasts).toHaveLength(1);
      expect(state.toasts[0].variant).toBe('success');
    });

    it('should provide toast.error method', () => {
      const { wrapper, store } = createWrapper();
      const { result } = renderHook(() => useToast(), { wrapper });

      act(() => {
        result.current.toast.error('Error via toast object');
      });

      const state = store.getState().toast;
      expect(state.toasts).toHaveLength(1);
      expect(state.toasts[0].variant).toBe('error');
    });

    it('should provide toast.warning method', () => {
      const { wrapper, store } = createWrapper();
      const { result } = renderHook(() => useToast(), { wrapper });

      act(() => {
        result.current.toast.warning('Warning via toast object');
      });

      const state = store.getState().toast;
      expect(state.toasts).toHaveLength(1);
      expect(state.toasts[0].variant).toBe('warning');
    });

    it('should provide toast.info method', () => {
      const { wrapper, store } = createWrapper();
      const { result } = renderHook(() => useToast(), { wrapper });

      act(() => {
        result.current.toast.info('Info via toast object');
      });

      const state = store.getState().toast;
      expect(state.toasts).toHaveLength(1);
      expect(state.toasts[0].variant).toBe('info');
    });

    it('should provide toast.clear method', () => {
      const { wrapper, store } = createWrapper();
      const { result } = renderHook(() => useToast(), { wrapper });

      let toastId: string = '';
      act(() => {
        toastId = result.current.toast.success('Test');
      });

      act(() => {
        result.current.toast.clear(toastId);
      });

      expect(store.getState().toast.toasts).toHaveLength(0);
    });

    it('should provide toast.clearAll method', () => {
      const { wrapper, store } = createWrapper();
      const { result } = renderHook(() => useToast(), { wrapper });

      act(() => {
        result.current.toast.success('Test 1');
        result.current.toast.error('Test 2');
      });

      act(() => {
        result.current.toast.clearAll();
      });

      expect(store.getState().toast.toasts).toHaveLength(0);
    });
  });

  describe('auto-removal', () => {
    it('should auto-remove toast after duration', () => {
      const { wrapper, store } = createWrapper();
      const { result } = renderHook(() => useToast(), { wrapper });

      act(() => {
        result.current.success('Test message', 3000);
      });

      expect(store.getState().toast.toasts).toHaveLength(1);

      act(() => {
        vi.advanceTimersByTime(3000);
      });

      expect(store.getState().toast.toasts).toHaveLength(0);
    });

    it('should not auto-remove toast with duration 0', () => {
      const { wrapper, store } = createWrapper();
      const { result } = renderHook(() => useToast(), { wrapper });

      act(() => {
        result.current.success('Persistent message', 0);
      });

      expect(store.getState().toast.toasts).toHaveLength(1);

      act(() => {
        vi.advanceTimersByTime(10000);
      });

      expect(store.getState().toast.toasts).toHaveLength(1);
    });
  });
});
