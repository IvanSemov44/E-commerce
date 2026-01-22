import { createApi, fetchBaseQuery } from '@reduxjs/toolkit/query/react';

const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000/api';

export interface WishlistItem {
  productId: string;
  addedAt: string;
}

export interface WishlistResponse {
  items: WishlistItem[];
}

export interface ApiResponse<T> {
  success: boolean;
  message: string;
  data?: T;
  errors?: string[];
}

export const wishlistApi = createApi({
  reducerPath: 'wishlistApi',
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
  tagTypes: ['Wishlist'],
  endpoints: (builder) => ({
    getWishlist: builder.query<WishlistResponse, void>({
      query: () => '/wishlist',
      transformResponse: (response: ApiResponse<WishlistResponse>) =>
        response.data || { items: [] },
      providesTags: ['Wishlist'],
    }),

    checkInWishlist: builder.query<boolean, string>({
      query: (productId) => `/wishlist/contains/${productId}`,
      transformResponse: (response: ApiResponse<boolean>) => response.data || false,
    }),

    addToWishlist: builder.mutation<WishlistResponse, string>({
      query: (productId) => ({
        url: '/wishlist/add',
        method: 'POST',
        body: { productId },
      }),
      transformResponse: (response: ApiResponse<WishlistResponse>) =>
        response.data || { items: [] },
      invalidatesTags: ['Wishlist'],
    }),

    removeFromWishlist: builder.mutation<WishlistResponse, string>({
      query: (productId) => ({
        url: `/wishlist/remove/${productId}`,
        method: 'DELETE',
      }),
      transformResponse: (response: ApiResponse<WishlistResponse>) =>
        response.data || { items: [] },
      invalidatesTags: ['Wishlist'],
    }),

    clearWishlist: builder.mutation<void, void>({
      query: () => ({
        url: '/wishlist/clear',
        method: 'POST',
      }),
      invalidatesTags: ['Wishlist'],
    }),
  }),
});

export const {
  useGetWishlistQuery,
  useCheckInWishlistQuery,
  useAddToWishlistMutation,
  useRemoveFromWishlistMutation,
  useClearWishlistMutation,
} = wishlistApi;
