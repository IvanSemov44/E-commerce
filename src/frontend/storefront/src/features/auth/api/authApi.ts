import type {
  AuthUser,
  LoginRequest,
  RegisterRequest,
  ForgotPasswordRequest,
  ResetPasswordRequest,
  ApiResponse,
} from '@/shared/types';
import { baseApi } from '@/shared/lib/api/baseApi';

const authApiSlice = baseApi.injectEndpoints({
  endpoints: (builder) => ({
    register: builder.mutation<
      { success: boolean; message: string; user?: AuthUser },
      RegisterRequest
    >({
      query: (credentials) => ({
        url: '/auth/register',
        method: 'POST',
        body: credentials,
      }),
      transformResponse: (response: ApiResponse<AuthUser>) => ({
        success: response.success,
        message: response.message,
        user: response.data,
      }),
    }),
    login: builder.mutation<{ success: boolean; message: string; user?: AuthUser }, LoginRequest>({
      query: (credentials) => ({
        url: '/auth/login',
        method: 'POST',
        body: credentials,
      }),
      transformResponse: (response: ApiResponse<AuthUser>) => ({
        success: response.success,
        message: response.message,
        user: response.data,
      }),
    }),
    refreshToken: builder.mutation<{ success: boolean; message: string; user?: AuthUser }, void>({
      query: () => ({
        url: '/auth/refresh-token',
        method: 'POST',
      }),
      transformResponse: (response: ApiResponse<AuthUser>) => ({
        success: response.success,
        message: response.message,
        user: response.data,
      }),
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
    getCurrentUser: builder.query<AuthUser | null, void>({
      query: () => '/auth/me',
      transformResponse: (response: ApiResponse<AuthUser>) => response.data || null,
    }),
    forgotPassword: builder.mutation<{ success: boolean; message: string }, ForgotPasswordRequest>({
      query: (data) => ({
        url: '/auth/forgot-password',
        method: 'POST',
        body: data,
      }),
      transformResponse: (response: ApiResponse<null>) => ({
        success: response.success,
        message: response.message,
      }),
    }),
    resetPassword: builder.mutation<{ success: boolean; message: string }, ResetPasswordRequest>({
      query: (data) => ({
        url: '/auth/reset-password',
        method: 'POST',
        body: data,
      }),
      transformResponse: (response: ApiResponse<null>) => ({
        success: response.success,
        message: response.message,
      }),
    }),
  }),
});

export const {
  useRegisterMutation,
  useLoginMutation,
  useRefreshTokenMutation,
  useLogoutMutation,
  useGetCurrentUserQuery,
  useForgotPasswordMutation,
  useResetPasswordMutation,
} = authApiSlice;
