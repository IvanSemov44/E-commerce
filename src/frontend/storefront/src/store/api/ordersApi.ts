import { createApi, fetchBaseQuery } from '@reduxjs/toolkit/query/react';
import {
  Order,
  OrderItem,
  CreateOrderRequest,
  CreateOrderItemRequest,
  OrderResponse,
  Address,
  ApiResponse,
  PaginatedResult,
} from '../../types';
import { config } from '../../config';

export const ordersApi = createApi({
  reducerPath: 'ordersApi',
  baseQuery: fetchBaseQuery({
    baseUrl: config.api.baseUrl,
    prepareHeaders: (headers) => {
      if (typeof window !== 'undefined') {
        const token = localStorage.getItem(config.storage.authToken);
        if (token) {
          headers.set('Authorization', `Bearer ${token}`);
        }
      }
      return headers;
    },
  }),
  keepUnusedDataFor: 60, // Keep cache for 60 seconds
  tagTypes: ['Order'],
  endpoints: (builder) => ({
    createOrder: builder.mutation<OrderResponse, CreateOrderRequest>({
      query: (orderData) => ({
        url: '/orders',
        method: 'POST',
        body: orderData,
      }),
      transformResponse: (response: ApiResponse<OrderResponse>) =>
        response.data || {} as OrderResponse,
      invalidatesTags: ['Order'],
    }),
    getOrders: builder.query<any[], void>({
      query: () => '/orders/my-orders',
      transformResponse: (response: ApiResponse<PaginatedResult<OrderListItem>>) => {
        return response.data?.items || [];
      },
      providesTags: ['Order'],
    }),
    getOrderById: builder.query<Order, string>({
      query: (id) => `/orders/${id}`,
      transformResponse: (response: ApiResponse<Order>) => response.data || {} as Order,
      providesTags: ['Order'],
    }),
    cancelOrder: builder.mutation<Order, string>({
      query: (orderId) => ({
        url: `/orders/${orderId}/cancel`,
        method: 'POST',
      }),
      transformResponse: (response: ApiResponse<Order>) => response.data || {} as Order,
      invalidatesTags: ['Order'],
    }),
  }),
});

export const {
  useCreateOrderMutation,
  useGetOrdersQuery,
  useGetOrderByIdQuery,
  useCancelOrderMutation,
} = ordersApi;
