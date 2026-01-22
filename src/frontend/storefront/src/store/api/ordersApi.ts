import { createApi, fetchBaseQuery } from '@reduxjs/toolkit/query/react';

const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000/api';

export interface OrderItem {
  productId: string;
  productName: string;
  price: number;
  quantity: number;
  imageUrl?: string;
}

export interface CreateOrderRequest {
  items: OrderItem[];
  shippingAddress: {
    firstName: string;
    lastName: string;
    phone: string;
    streetLine1: string;
    city: string;
    state: string;
    postalCode: string;
    country: string;
  };
  billingAddress?: {
    firstName: string;
    lastName: string;
    streetLine1: string;
    city: string;
    state: string;
    postalCode: string;
    country: string;
  };
  paymentMethod: string;
  promoCode?: string;
}

export interface OrderResponse {
  orderId: string;
  orderNumber: string;
  clientSecret?: string;
  totals: {
    subtotal: number;
    discount?: number;
    shipping: number;
    tax: number;
    total: number;
  };
}

export interface Order {
  id: string;
  orderNumber: string;
  userId: string;
  status: 'Pending' | 'Processing' | 'Shipped' | 'Delivered' | 'Cancelled';
  items: OrderItem[];
  shippingAddress: {
    firstName: string;
    lastName: string;
    email: string;
    phone: string;
    street: string;
    city: string;
    state: string;
    zipCode: string;
    country: string;
  };
  billingAddress?: {
    firstName: string;
    lastName: string;
    street: string;
    city: string;
    state: string;
    zipCode: string;
    country: string;
  };
  paymentMethod: string;
  totals: {
    subtotal: number;
    discount?: number;
    shipping: number;
    tax: number;
    total: number;
  };
  createdAt: string;
  updatedAt: string;
}

export interface ApiResponse<T> {
  success: boolean;
  message: string;
  data?: T;
  errors?: string[];
}

export interface PaginatedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface OrderListItem {
  id: string;
  orderNumber: string;
  status: 'Pending' | 'Processing' | 'Shipped' | 'Delivered' | 'Cancelled';
  paymentStatus: string;
  totalAmount: number;
  createdAt: string;
  items: OrderItem[];
}

export const ordersApi = createApi({
  reducerPath: 'ordersApi',
  baseQuery: fetchBaseQuery({
    baseUrl: API_URL,
    prepareHeaders: (headers) => {
      if (typeof window !== 'undefined') {
        const token = localStorage.getItem('authToken');
        if (token) {
          headers.set('Authorization', `Bearer ${token}`);
        }
      }
      return headers;
    },
  }),
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
