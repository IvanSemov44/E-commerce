import { createApi } from '@reduxjs/toolkit/query/react';
import type {
  Product,
  ProductDetail,
  CreateProductRequest,
  UpdateProductRequest,
  PaginatedResult,
  ApiResponse,
} from '@shared/types';
import { csrfBaseQuery } from '../../utils/apiFactory';

export const productsApi = createApi({
  reducerPath: 'productsApi',
  baseQuery: csrfBaseQuery,
  tagTypes: ['Product'],
  endpoints: (builder) => ({
    getProducts: builder.query<
      PaginatedResult<Product>,
      { page?: number; pageSize?: number; search?: string }
    >({
      query: ({ page = 1, pageSize = 20, search }) => {
        const params = new URLSearchParams();
        params.set('page', page.toString());
        params.set('pageSize', pageSize.toString());
        if (search) params.set('search', search);
        return `/products?${params}`;
      },
      transformResponse: (response: ApiResponse<PaginatedResult<Product>>) =>
        response.data || { items: [], totalCount: 0, page: 1, pageSize: 20 },
      providesTags: ['Product'],
    }),
    getProductById: builder.query<ProductDetail, string>({
      query: (id) => `/products/${id}`,
      transformResponse: (response: ApiResponse<ProductDetail>) =>
        response.data || ({} as ProductDetail),
      providesTags: ['Product'],
    }),
    createProduct: builder.mutation<ProductDetail, CreateProductRequest>({
      query: (product) => ({
        url: '/products',
        method: 'POST',
        body: product,
      }),
      invalidatesTags: ['Product'],
    }),
    updateProduct: builder.mutation<ProductDetail, UpdateProductRequest>({
      query: ({ id, ...product }) => ({
        url: `/products/${id}`,
        method: 'PUT',
        body: product,
      }),
      invalidatesTags: ['Product'],
    }),
    deleteProduct: builder.mutation<{ success: boolean }, string>({
      query: (id) => ({
        url: `/products/${id}`,
        method: 'DELETE',
      }),
      invalidatesTags: ['Product'],
    }),
    updateProductStock: builder.mutation<
      ProductDetail,
      { productId: string; quantity: number }
    >({
      query: ({ productId, quantity }) => ({
        url: `/products/${productId}/stock`,
        method: 'PUT',
        body: { quantity },
      }),
      invalidatesTags: ['Product'],
    }),
  }),
});

export const {
  useGetProductsQuery,
  useGetProductByIdQuery,
  useCreateProductMutation,
  useUpdateProductMutation,
  useDeleteProductMutation,
  useUpdateProductStockMutation,
} = productsApi;
