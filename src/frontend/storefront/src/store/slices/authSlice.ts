import { createSlice } from '@reduxjs/toolkit';
import type { PayloadAction } from '@reduxjs/toolkit';

export interface AuthUser {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  role: string;
  phone?: string;
  avatarUrl?: string;
}

export interface AuthState {
  isAuthenticated: boolean;
  user: AuthUser | null;
  loading: boolean;
  error: string | null;
  initialized: boolean;
}

const initialState: AuthState = {
  isAuthenticated: false,
  user: null,
  loading: false,
  error: null,
  initialized: false,
};

export const authSlice = createSlice({
  name: 'auth',
  initialState,
  reducers: {
    loginStart: (state) => {
      state.loading = true;
      state.error = null;
    },
    loginSuccess: (state, action: PayloadAction<AuthUser>) => {
      state.isAuthenticated = true;
      state.user = action.payload;
      state.loading = false;
      state.initialized = true;
    },
    loginFailure: (state, action: PayloadAction<string>) => {
      state.loading = false;
      state.error = action.payload;
      state.initialized = true;
    },
    logout: (state) => {
      state.isAuthenticated = false;
      state.user = null;
      state.error = null;
      state.initialized = true;
    },
    clearError: (state) => {
      state.error = null;
    },
    updateUser: (state, action: PayloadAction<Partial<AuthUser>>) => {
      if (state.user) {
        state.user = { ...state.user, ...action.payload };
      }
    },
    setUser: (state, action: PayloadAction<AuthUser>) => {
      state.user = action.payload;
      state.isAuthenticated = true;
      state.initialized = true;
    },
    setInitialized: (state) => {
      state.initialized = true;
    },
  },
});

export const { loginStart, loginSuccess, loginFailure, logout, clearError, updateUser, setUser, setInitialized } = authSlice.actions;
export const authReducer = authSlice.reducer;
