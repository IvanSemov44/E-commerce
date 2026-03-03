import type {
  Order,
  OrderResponse,
  CreateOrderRequest,
  ApiResponse,
  PaginatedResult,
} from '@/shared/types';
import { baseApi } from '@/shared/lib/api/baseApi';

const ordersApiSlice = baseApi.injectEndpoints({
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
      transformResponse: (response: ApiResponse<PaginatedResult<Order>>) => {
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
} = ordersApiSlice;
