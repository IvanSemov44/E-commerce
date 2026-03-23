/**
 * Toast Slice - Redux state management for toast notifications
 * Handles add, remove, clear operations for toast messages
 */

import { createSlice } from '@reduxjs/toolkit';
import type { PayloadAction } from '@reduxjs/toolkit';

export type ToastVariant = 'success' | 'error' | 'warning' | 'info';

export interface Toast {
  id: string;
  message: string;
  variant: ToastVariant;
  duration?: number;
}

export interface ToastState {
  toasts: Toast[];
}

const initialState: ToastState = {
  toasts: [],
};

export const toastSlice = createSlice({
  name: 'toast',
  initialState,
  reducers: {
    addToast: (state, action: PayloadAction<Toast>) => {
      state.toasts.push(action.payload);
    },
    removeToast: (state, action: PayloadAction<string>) => {
      state.toasts = state.toasts.filter((toast) => toast.id !== action.payload);
    },
    clearToasts: (state) => {
      state.toasts = [];
    },
  },
});

export const { addToast, removeToast, clearToasts } = toastSlice.actions;
export const toastReducer = toastSlice.reducer;
