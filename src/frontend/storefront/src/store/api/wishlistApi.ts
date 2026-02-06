import { createApi, fetchBaseQuery } from '@reduxjs/toolkit/query/react';
import { WishlistItem, WishlistResponse, ApiResponse } from '../../types';
import { config } from '../../config';

export const wishlistApi = createApi({
  reducerPath: 'wishlistApi',
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
  tagTypes: ['Wishlist', 'WishlistCheck'],
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
      providesTags: (_result, _error, productId) => [{ type: 'WishlistCheck', id: productId }],
    }),

    addToWishlist: builder.mutation<WishlistResponse, string>({
      query: (productId) => ({
        url: '/wishlist/add',
        method: 'POST',
        body: { productId },
      }),
      transformResponse: (response: ApiResponse<WishlistResponse>) =>
        response.data || { items: [] },
      invalidatesTags: (_result, _error, productId) => [
        'Wishlist',
        { type: 'WishlistCheck', id: productId },
      ],
    }),

    removeFromWishlist: builder.mutation<WishlistResponse, string>({
      query: (productId) => ({
        url: `/wishlist/remove/${productId}`,
        method: 'DELETE',
      }),
      transformResponse: (response: ApiResponse<WishlistResponse>) =>
        response.data || { items: [] },
      invalidatesTags: (_result, _error, productId) => [
        'Wishlist',
        { type: 'WishlistCheck', id: productId },
      ],
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
