import { createApi, fetchBaseQuery } from '@reduxjs/toolkit/query/react';
import type { BaseQueryFn, FetchArgs, FetchBaseQueryError } from '@reduxjs/toolkit/query';
import { config } from '@/config';
import { logout } from '@/features/auth/slices/authSlice';
import { telemetry } from '@/shared/lib/utils/telemetry';

/**
 * Helper function to get CSRF token from cookie
 */
const getCsrfToken = (): string | null => {
  if (typeof document === 'undefined') return null;
  const match = document.cookie.match(/XSRF-TOKEN=([^;]+)/);
  return match ? decodeURIComponent(match[1]) : null;
};

const baseQuery = fetchBaseQuery({
  baseUrl: config.api.baseUrl,
  credentials: 'include', // Required for httpOnly cookies to be sent
  prepareHeaders: (headers) => {
    // Add CSRF token header for state-changing requests
    const csrfToken = getCsrfToken();
    if (csrfToken) {
      headers.set('X-XSRF-TOKEN', csrfToken);
    }
    return headers;
  },
});

const baseQueryWithReauth: BaseQueryFn<string | FetchArgs, unknown, FetchBaseQueryError> = async (
  args,
  api,
  extraOptions
) => {
  const result = await baseQuery(args, api, extraOptions);
  if (result.error && result.error.status === 401) {
    // Try to refresh the token
    const refreshResult = await baseQuery(
      { url: '/auth/refresh-token', method: 'POST' },
      api,
      extraOptions
    );
    
    if (refreshResult.error) {
      // Refresh failed, logout user
      api.dispatch(logout());
    } else {
      // Token refreshed, retry original request
      return baseQuery(args, api, extraOptions);
    }
  }
  return result;
};

/**
 * Outermost query wrapper: measures duration and records an api.request telemetry
 * event for every request regardless of success or failure.
 */
const baseQueryWithTelemetry: BaseQueryFn<string | FetchArgs, unknown, FetchBaseQueryError> = async (
  args,
  api,
  extraOptions
) => {
  const startedAt = performance.now();
  const result = await baseQueryWithReauth(args, api, extraOptions);
  const durationMs = Math.round(performance.now() - startedAt);

  telemetry.track('api.request', {
    endpoint: typeof args === 'string' ? args : (args as FetchArgs).url,
    method: typeof args === 'string' ? 'GET' : ((args as FetchArgs).method ?? 'GET'),
    success: !result.error,
    durationMs,
  });

  return result;
};

export const baseApi = createApi({
  reducerPath: 'api',
  baseQuery: baseQueryWithTelemetry,
  keepUnusedDataFor: 60,
  tagTypes: ['Cart', 'Order', 'Profile', 'Review', 'Wishlist', 'WishlistCheck', 'Categories', 'Products', 'User', 'Auth'],
  endpoints: () => ({}),
});
