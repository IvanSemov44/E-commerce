import { createApi, fetchBaseQuery } from '@reduxjs/toolkit/query/react';
import type { User, PaginatedResult, ApiResponse } from '@shared/types';

const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000/api';

/**
 * Helper function to get CSRF token from cookie
 */
const getCsrfToken = (): string | null => {
  if (typeof document === 'undefined') return null;
  const match = document.cookie.match(/XSRF-TOKEN=([^;]+)/);
  return match ? decodeURIComponent(match[1]) : null;
};

export const customersApi = createApi({
  reducerPath: 'customersApi',
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
  tagTypes: ['Customer'],
  endpoints: (builder) => ({
    getCustomers: builder.query<
      PaginatedResult<User>,
      { page?: number; pageSize?: number; search?: string }
    >({
      query: ({ page = 1, pageSize = 20, search }) => {
        const params = new URLSearchParams();
        params.set('page', page.toString());
        params.set('pageSize', pageSize.toString());
        if (search) params.set('search', search);
        return `/customers?${params}`;
      },
      transformResponse: (response: ApiResponse<PaginatedResult<User>>) =>
        response.data || { items: [], totalCount: 0, page: 1, pageSize: 20 },
      providesTags: ['Customer'],
    }),
    getCustomerById: builder.query<User, string>({
      query: (id) => `/customers/${id}`,
      transformResponse: (response: ApiResponse<User>) =>
        response.data || ({} as User),
      providesTags: ['Customer'],
    }),
    getCustomerStats: builder.query<
      {
        totalCustomers: number;
        activeCustomers: number;
        newCustomersThisMonth: number;
      },
      void
    >({
      query: () => '/customers/stats',
      transformResponse: (response: ApiResponse<Record<string, unknown>>) =>
        response.data || {
          totalCustomers: 0,
          activeCustomers: 0,
          newCustomersThisMonth: 0,
        },
    }),
  }),
});

export const {
  useGetCustomersQuery,
  useGetCustomerByIdQuery,
  useGetCustomerStatsQuery,
} = customersApi;
