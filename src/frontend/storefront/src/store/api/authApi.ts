import type {
  AuthUser,
  LoginRequest,
  RegisterRequest,
  ForgotPasswordRequest,
  ResetPasswordRequest,
  AuthResponse,
  ApiResponse,
} from '../../types';
import { baseApi } from './baseApi';

interface AuthData {
  user: AuthUser;
  token: string;
}

const authApiSlice = baseApi.injectEndpoints({
  endpoints: (builder) => ({
    register: builder.mutation<AuthResponse, RegisterRequest>({
      query: (credentials) => ({
        url: '/auth/register',
        method: 'POST',
        body: credentials,
      }),
      transformResponse: (response: ApiResponse<AuthData>) => ({
        success: response.success,
        message: response.message,
        user: response.data?.user,
        token: response.data?.token,
      }),
    }),
    login: builder.mutation<AuthResponse, LoginRequest>({
      query: (credentials) => ({
        url: '/auth/login',
        method: 'POST',
        body: credentials,
      }),
      transformResponse: (response: ApiResponse<AuthData>) => ({
        success: response.success,
        message: response.message,
        user: response.data?.user,
        token: response.data?.token,
      }),
    }),
    refreshToken: builder.mutation<AuthResponse, string>({
      query: (token) => ({
        url: '/auth/refresh-token',
        method: 'POST',
        body: { token },
      }),
      transformResponse: (response: ApiResponse<AuthData>) => ({
        success: response.success,
        message: response.message,
        user: response.data?.user,
        token: response.data?.token,
      }),
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
  useForgotPasswordMutation,
  useResetPasswordMutation,
} = authApiSlice;
