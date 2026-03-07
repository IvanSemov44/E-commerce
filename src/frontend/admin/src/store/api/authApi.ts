import { createApi } from '@reduxjs/toolkit/query/react';
import type { LoginRequest, RegisterRequest, ApiResponse } from '@shared/types';
import type { AdminUser } from '../slices/authSlice';
import { csrfBaseQuery } from '../../utils/apiFactory';

interface AuthResponse {
  success: boolean;
  message: string;
  user?: AdminUser;
}

export const authApi = createApi({
  reducerPath: 'authApi',
  baseQuery: csrfBaseQuery,
  endpoints: (builder) => ({
    login: builder.mutation<AuthResponse, LoginRequest>({
      query: (credentials) => ({
        url: '/auth/login',
        method: 'POST',
        body: credentials,
      }),
      transformResponse: (response: ApiResponse<AdminUser>) => ({
        success: response.success,
        message: response.message,
        user: response.data,
      }),
    }),
    register: builder.mutation<AuthResponse, RegisterRequest>({
      query: (credentials) => ({
        url: '/auth/register',
        method: 'POST',
        body: credentials,
      }),
      transformResponse: (response: ApiResponse<AdminUser>) => ({
        success: response.success,
        message: response.message,
        user: response.data,
      }),
    }),
    getCurrentUser: builder.query<AdminUser | null, void>({
      query: () => '/auth/me',
      transformResponse: (response: ApiResponse<AdminUser>) => response.data || null,
    }),
    logout: builder.mutation<{ success: boolean; message: string }, void>({
      query: () => ({
        url: '/auth/logout',
        method: 'POST',
      }),
      transformResponse: (response: ApiResponse<null>) => ({
        success: response.success,
        message: response.message,
      }),
    }),
    refreshToken: builder.mutation<AuthResponse, void>({
      query: () => ({
        url: '/auth/refresh-token',
        method: 'POST',
      }),
      transformResponse: (response: ApiResponse<AdminUser>) => ({
        success: response.success,
        message: response.message,
        user: response.data,
      }),
    }),
  }),
});

export const {
  useLoginMutation,
  useRegisterMutation,
  useGetCurrentUserQuery,
  useLogoutMutation,
  useRefreshTokenMutation,
} = authApi;
