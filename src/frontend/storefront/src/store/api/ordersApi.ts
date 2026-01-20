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

export interface ApiResponse<T> {
  success: boolean;
  message: string;
  data?: T;
  errors?: string[];
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
  endpoints: (builder) => ({
    createOrder: builder.mutation<OrderResponse, CreateOrderRequest>({
      query: (orderData) => ({
        url: '/checkout',
        method: 'POST',
        body: orderData,
      }),
      transformResponse: (response: ApiResponse<OrderResponse>) =>
        response.data || {} as OrderResponse,
    }),
    getOrders: builder.query<any[], void>({
      query: () => '/orders',
      transformResponse: (response: ApiResponse<any[]>) => response.data || [],
    }),
    getOrderById: builder.query<any, string>({
      query: (id) => `/orders/${id}`,
      transformResponse: (response: ApiResponse<any>) => response.data || {},
    }),
  }),
});

export const {
  useCreateOrderMutation,
  useGetOrdersQuery,
  useGetOrderByIdQuery,
} = ordersApi;
