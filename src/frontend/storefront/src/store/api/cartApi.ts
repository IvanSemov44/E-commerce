import type {
  CartDto,
  AddToCartRequest,
  UpdateCartItemRequest,
  ApiResponse,
} from '../../types';
import { baseApi } from './baseApi';

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
} = cartApiSlice;
