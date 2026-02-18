import { createApi, fetchBaseQuery } from '@reduxjs/toolkit/query/react';
import type {
  Product,
  ProductDetail,
  CreateProductRequest,
  UpdateProductRequest,
  PaginatedResult,
  ApiResponse,
} from '../../types';

const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000/api';

/**
 * Helper function to get CSRF token from cookie
 */
const getCsrfToken = (): string | null => {
  if (typeof document === 'undefined') return null;
  const match = document.cookie.match(/XSRF-TOKEN=([^;]+)/);
  return match ? decodeURIComponent(match[1]) : null;
};

export const productsApi = createApi({
  reducerPath: 'productsApi',
  baseQuery: fetchBaseQuery({
    baseUrl: API_URL,
    credentials: 'include', // Required for httpOnly cookies to be sent
    prepareHeaders: (headers) => {
      // Add CSRF token header for state-changing requests
      const csrfToken = getCsrfToken();
      if (csrfToken) {
        headers.set('X-XSRF-TOKEN', csrfToken);
      }
      return headers;
    },
  }),
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
