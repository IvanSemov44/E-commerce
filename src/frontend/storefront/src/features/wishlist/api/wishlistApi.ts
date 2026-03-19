import type { ApiResponse } from '@/shared/types';
import { baseApi } from '@/shared/lib/api/baseApi';

interface WishlistItem {
  id: string;
  productId: string;
  productName: string;
  productImage?: string;
  price: number;
  compareAtPrice?: number;
  stockQuantity: number;
  isAvailable: boolean;
  addedAt: string;
}

interface WishlistResponse {
  id: string;
  items: WishlistItem[];
  itemCount: number;
}

const wishlistApiSlice = baseApi.injectEndpoints({
  endpoints: (builder) => ({
    getWishlist: builder.query<WishlistResponse, void>({
      query: () => '/wishlist',
      transformResponse: (response: ApiResponse<WishlistResponse>) =>
        response.data || { id: '', items: [], itemCount: 0 },
      providesTags: ['Wishlist'],
    }),

    addToWishlist: builder.mutation<void, string>({
      query: (productId) => ({
        url: '/wishlist/add',
        method: 'POST',
        body: { productId },
      }),
      invalidatesTags: ['Wishlist'],
    }),

    removeFromWishlist: builder.mutation<void, string>({
      query: (productId) => ({
        url: `/wishlist/remove/${productId}`,
        method: 'DELETE',
      }),
      async onQueryStarted(productId, { dispatch, queryFulfilled }) {
        const listPatch = dispatch(
          wishlistApiSlice.util.updateQueryData('getWishlist', undefined, (draft) => {
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
          listPatch.undo();
        }
      },
      invalidatesTags: ['Wishlist'],
    }),
  }),
});

export const { useGetWishlistQuery, useAddToWishlistMutation, useRemoveFromWishlistMutation } =
  wishlistApiSlice;
