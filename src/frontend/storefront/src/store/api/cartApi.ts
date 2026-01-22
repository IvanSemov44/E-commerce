import { createApi, fetchBaseQuery } from '@reduxjs/toolkit/query/react';

const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000/api';

export interface CartItemDto {
  cartItemId: string;
  productId: string;
  productName: string;
  quantity: number;
  price: number;
  imageUrl?: string;
}

export interface CartDto {
  id: string;
  items: CartItemDto[];
  subtotal: number;
  itemCount: number;
}

export interface AddToCartRequest {
  productId: string;
  quantity: number;
}

export interface UpdateCartItemRequest {
  cartItemId: string;
  quantity: number;
}

export interface ApiResponse<T> {
  success: boolean;
  message: string;
  data?: T;
  errors?: string[];
}

export const cartApi = createApi({
  reducerPath: 'cartApi',
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
  tagTypes: ['Cart'],
  endpoints: (builder) => ({
    getCart: builder.query<CartDto, void>({
      query: () => '/cart',
      transformResponse: (response: ApiResponse<CartDto>) =>
        response.data || { id: '', items: [], subtotal: 0, itemCount: 0 },
      providesTags: ['Cart'],
    }),

    addToCart: builder.mutation<CartDto, AddToCartRequest>({
      query: (data) => ({
        url: '/cart/add-item',
        method: 'POST',
        body: data,
      }),
      transformResponse: (response: ApiResponse<CartDto>) =>
        response.data || { id: '', items: [], subtotal: 0, itemCount: 0 },
      invalidatesTags: ['Cart'],
    }),

    updateCartItem: builder.mutation<CartDto, UpdateCartItemRequest>({
      query: (data) => ({
        url: `/cart/update-item/${data.cartItemId}`,
        method: 'PUT',
        body: { quantity: data.quantity },
      }),
      transformResponse: (response: ApiResponse<CartDto>) =>
        response.data || { id: '', items: [], subtotal: 0, itemCount: 0 },
      invalidatesTags: ['Cart'],
    }),

    removeFromCart: builder.mutation<CartDto, string>({
      query: (cartItemId) => ({
        url: `/cart/remove-item/${cartItemId}`,
        method: 'DELETE',
      }),
      transformResponse: (response: ApiResponse<CartDto>) =>
        response.data || { id: '', items: [], subtotal: 0, itemCount: 0 },
      invalidatesTags: ['Cart'],
    }),

    clearCart: builder.mutation<CartDto, void>({
      query: () => ({
        url: '/cart/clear',
        method: 'POST',
      }),
      transformResponse: (response: ApiResponse<CartDto>) =>
        response.data || { id: '', items: [], subtotal: 0, itemCount: 0 },
      invalidatesTags: ['Cart'],
    }),
  }),
});

export const {
  useGetCartQuery,
  useAddToCartMutation,
  useUpdateCartItemMutation,
  useRemoveFromCartMutation,
  useClearCartMutation,
} = cartApi;
