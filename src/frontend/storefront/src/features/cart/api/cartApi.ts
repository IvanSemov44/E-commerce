import type {
  CartDto,
  AddToCartRequest,
  UpdateCartItemRequest,
  ApiResponse,
} from '@/shared/types';
import { baseApi } from '@/shared/lib/api/baseApi';

const cartApiSlice = baseApi.injectEndpoints({
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
      async onQueryStarted({ cartItemId, quantity }, { dispatch, queryFulfilled }) {
        const patchResult = dispatch(
          baseApi.util.updateQueryData('getCart', undefined, (draft) => {
            const item = draft.items.find((i) => i.id === cartItemId);
            if (item) {
              const delta = quantity - item.quantity;
              item.quantity = quantity;
              draft.subtotal += item.price * delta;
              draft.itemCount += delta;
            }
          })
        );
        try {
          await queryFulfilled;
        } catch {
          patchResult.undo();
        }
      },
      invalidatesTags: ['Cart'],
    }),

    removeFromCart: builder.mutation<CartDto, string>({
      query: (cartItemId) => ({
        url: `/cart/remove-item/${cartItemId}`,
        method: 'DELETE',
      }),
      transformResponse: (response: ApiResponse<CartDto>) =>
        response.data || { id: '', items: [], subtotal: 0, itemCount: 0 },
      async onQueryStarted(cartItemId, { dispatch, queryFulfilled }) {
        const patchResult = dispatch(
          baseApi.util.updateQueryData('getCart', undefined, (draft) => {
            const idx = draft.items.findIndex((item) => item.id === cartItemId);
            if (idx !== -1) {
              const removed = draft.items[idx];
              draft.subtotal -= removed.price * removed.quantity;
              draft.itemCount -= removed.quantity;
              draft.items.splice(idx, 1);
            }
          })
        );
        try {
          await queryFulfilled;
        } catch {
          patchResult.undo();
        }
      },
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
} = cartApiSlice;
