import { createApi, fetchBaseQuery } from '@reduxjs/toolkit/query/react';
import type { Order, OrderStatus, PaginatedResult, ApiResponse } from '@shared/types';

const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000/api';

/**
 * Helper function to get CSRF token from cookie
 */
const getCsrfToken = (): string | null => {
  if (typeof document === 'undefined') return null;
  const match = document.cookie.match(/XSRF-TOKEN=([^;]+)/);
  return match ? decodeURIComponent(match[1]) : null;
};

export const ordersApi = createApi({
  reducerPath: 'ordersApi',
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
  tagTypes: ['Order'],
  endpoints: (builder) => ({
    getOrders: builder.query<
      PaginatedResult<Order>,
      { page?: number; pageSize?: number; status?: OrderStatus; search?: string }
    >({
      query: ({ page = 1, pageSize = 20, status, search }) => {
        const params = new URLSearchParams();
        params.set('page', page.toString());
        params.set('pageSize', pageSize.toString());
        if (status) params.set('status', status);
        if (search) params.set('search', search);
        return `/orders?${params}`;
      },
      transformResponse: (response: ApiResponse<PaginatedResult<Order>>) =>
        response.data || { items: [], totalCount: 0, page: 1, pageSize: 20 },
      providesTags: ['Order'],
    }),
    getOrderById: builder.query<Order, string>({
      query: (id) => `/orders/${id}`,
      transformResponse: (response: ApiResponse<Order>) =>
        response.data || ({} as Order),
      providesTags: ['Order'],
    }),
    updateOrderStatus: builder.mutation<
      Order,
      { orderId: string; status: OrderStatus; trackingNumber?: string }
    >({
      query: ({ orderId, status, trackingNumber }) => ({
        url: `/orders/${orderId}/status`,
        method: 'PUT',
        body: { status, trackingNumber },
      }),
      invalidatesTags: ['Order'],
    }),
    getOrderStats: builder.query<
      {
        totalOrders: number;
        totalRevenue: number;
        ordersToday: number;
        pendingOrders: number;
      },
      void
    >({
      query: () => '/orders/stats',
      transformResponse: (response: ApiResponse<Record<string, unknown>>) =>
        response.data || {
          totalOrders: 0,
          totalRevenue: 0,
          ordersToday: 0,
          pendingOrders: 0,
        },
    }),
  }),
});

export const {
  useGetOrdersQuery,
  useGetOrderByIdQuery,
  useUpdateOrderStatusMutation,
  useGetOrderStatsQuery,
} = ordersApi;
