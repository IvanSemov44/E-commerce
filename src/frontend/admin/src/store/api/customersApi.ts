import { createApi } from '@reduxjs/toolkit/query/react';
import type { User, PaginatedResult, ApiResponse } from '@shared/types';
import { csrfBaseQuery } from '../../utils/apiFactory';

export const customersApi = createApi({
  reducerPath: 'customersApi',
  baseQuery: csrfBaseQuery,
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
      transformResponse: (response: ApiResponse<{ totalCustomers: number; activeCustomers: number; newCustomersThisMonth: number }>) =>
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
