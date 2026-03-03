import { createApi, fetchBaseQuery } from '@reduxjs/toolkit/query/react';
import type { DashboardStats, ApiResponse } from '@shared/types';

const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000/api';

/**
 * Helper function to get CSRF token from cookie
 */
const getCsrfToken = (): string | null => {
  if (typeof document === 'undefined') return null;
  const match = document.cookie.match(/XSRF-TOKEN=([^;]+)/);
  return match ? decodeURIComponent(match[1]) : null;
};

export const dashboardApi = createApi({
  reducerPath: 'dashboardApi',
  baseQuery: fetchBaseQuery({
    baseUrl: API_URL,
    credentials: 'include', // Required for httpOnly cookies to be sent
    prepareHeaders: (headers) => {
      // Add CSRF token header for state-changing requests
      const csrfToken = getCsrfToken();
      if (csrfToken) {
        headers.set('X-XSRF-TOKEN', csrfToken);
      }
      return headers;
    },
  }),
  endpoints: (builder) => ({
    getDashboardStats: builder.query<DashboardStats, void>({
      query: () => '/dashboard/stats',
      transformResponse: (response: ApiResponse<DashboardStats>) =>
        response.data || {
          totalOrders: 0,
          totalRevenue: 0,
          totalCustomers: 0,
          totalProducts: 0,
          ordersTrend: [],
          revenueTrend: [],
        },
    }),
  }),
});

export const { useGetDashboardStatsQuery } = dashboardApi;
