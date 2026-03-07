import type { WishlistResponse, ApiResponse } from '@/shared/types';
import { baseApi } from '@/shared/lib/api/baseApi';

const wishlistApiSlice = baseApi.injectEndpoints({
  endpoints: (builder) => ({
    getWishlist: builder.query<WishlistResponse, void>({
      query: () => '/wishlist',
      transformResponse: (response: ApiResponse<WishlistResponse>) =>
        response.data || { id: '', items: [], itemCount: 0 },
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
        response.data || { id: '', items: [], itemCount: 0 },
      async onQueryStarted(productId, { dispatch, queryFulfilled }) {
        const patchResult = dispatch(
          baseApi.util.updateQueryData('checkInWishlist', productId, () => true)
        );
        try {
          await queryFulfilled;
        } catch {
          patchResult.undo();
        }
      },
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
        response.data || { id: '', items: [], itemCount: 0 },
      async onQueryStarted(productId, { dispatch, queryFulfilled }) {
        const checkPatch = dispatch(
          baseApi.util.updateQueryData('checkInWishlist', productId, () => false)
        );
        const listPatch = dispatch(
          baseApi.util.updateQueryData('getWishlist', undefined, (draft) => {
            const idx = draft.items.findIndex((item) => item.productId === productId);
            if (idx !== -1) {
              draft.items.splice(idx, 1);
              draft.itemCount = draft.items.length;
            }
          })
        );
        try {
          await queryFulfilled;
        } catch {
          checkPatch.undo();
          listPatch.undo();
        }
      },
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
