import { createApi } from '@reduxjs/toolkit/query/react';
import type { DashboardStats, ApiResponse } from '@shared/types';
import { csrfBaseQuery } from '../../utils/apiFactory';

export const dashboardApi = createApi({
  reducerPath: 'dashboardApi',
  baseQuery: csrfBaseQuery,
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
