import type { WishlistResponse, ApiResponse } from '../../types';
import { baseApi } from './baseApi';

const wishlistApiSlice = baseApi.injectEndpoints({
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
} = wishlistApiSlice;
