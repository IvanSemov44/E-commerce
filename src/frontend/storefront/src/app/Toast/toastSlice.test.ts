import { describe, it, expect } from 'vitest';
import {
  toastReducer as reducer,
  addToast,
  removeToast,
  clearToasts,
  type ToastState,
} from './toastSlice';

describe('toastSlice', () => {
  it('adds toast', () => {
    const initial: ToastState = { toasts: [] };
    const next = reducer(
      initial,
      addToast({ id: '1', message: 'Saved', variant: 'success', duration: 1000 })
    );

    expect(next.toasts).toHaveLength(1);
    expect(next.toasts[0].message).toBe('Saved');
  });

  it('removes toast by id', () => {
    const initial: ToastState = {
      toasts: [
        { id: '1', message: 'A', variant: 'info' },
        { id: '2', message: 'B', variant: 'error' },
      ],
    };

    const next = reducer(initial, removeToast('1'));
    expect(next.toasts).toHaveLength(1);
    expect(next.toasts[0].id).toBe('2');
  });

  it('clears all toasts', () => {
    const initial: ToastState = {
      toasts: [{ id: '1', message: 'A', variant: 'info' }],
    };

    const next = reducer(initial, clearToasts());
    expect(next.toasts).toEqual([]);
  });
});
