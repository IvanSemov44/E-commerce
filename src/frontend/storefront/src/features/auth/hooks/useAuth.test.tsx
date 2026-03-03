import { describe, it, expect, vi, beforeEach } from 'vitest';
import { renderHook, act } from '@testing-library/react';
import { configureStore } from '@reduxjs/toolkit';
import { Provider } from 'react-redux';
import { useAuth } from './useAuth';
import { authSlice } from '../slices/authSlice';
import type { AuthUser } from '../../../../types';

// Mock useErrorHandler
vi.mock('../../../hooks/useErrorHandler', () => ({
  useErrorHandler: () => ({
    handleError: vi.fn((error) => ({ message: String(error) })),
    clearError: vi.fn(),
  }),
}));

const createTestStore = (initialAuthState: Partial<{
  isAuthenticated: boolean;
  user: AuthUser | null;
  loading: boolean;
  error: string | null;
  initialized: boolean;
}> = {})