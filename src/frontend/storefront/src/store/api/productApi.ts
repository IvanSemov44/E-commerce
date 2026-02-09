import { createApi, fetchBaseQuery } from '@reduxjs/toolkit/query/react';
import type {
  Product,
  ProductDetail,
  PaginatedResult,
  ApiResponse,
} from '../../types';
import { config } from '../../config';

// Re-export types for components
export type { Product, ProductDetail, ProductImage, ProductCategory } from '../../types';

export const productApi = createApi({
  reducerPath: 'productApi',
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
  endpoints: (builder) => ({
    getProducts: builder.query<
      PaginatedResult<Product>,
      {
        page?: number;
        pageSize?: number;
        categoryId?: string;
        search?: string;
        minPrice?: number;
        maxPrice?: number;
        minRating?: number;
        isFeatured?: boolean;
        sortBy?: string;
      }
    >({
      query: ({ page = 1, pageSize = 20, categoryId, search, minPrice, maxPrice, minRating, isFeatured, sortBy }) => {
        const params = new URLSearchParams();
        params.set('page', page.toString());
        params.set('pageSize', pageSize.toString());
        if (categoryId) params.set('categoryId', categoryId);
        if (search) params.set('search', search);
        if (minPrice !== undefined) params.set('minPrice', minPrice.toString());
        if (maxPrice !== undefined) params.set('maxPrice', maxPrice.toString());
        if (minRating !== undefined) params.set('minRating', minRating.toString());
        if (isFeatured !== undefined) params.set('isFeatured', isFeatured.toString());
        if (sortBy) params.set('sortBy', sortBy);
        return `/products?${params}`;
      },
      transformResponse: (response: ApiResponse<PaginatedResult<Product>>) =>
        response.data || { items: [], totalCount: 0, page: 1, pageSize: 20, totalPages: 0, hasNext: false, hasPrevious: false },
    }),
    getProductBySlug: builder.query<ProductDetail, string>({
      query: (slug) => `/products/slug/${slug}`,
      transformResponse: (response: ApiResponse<ProductDetail>) => response.data || {} as ProductDetail,
    }),
    getProductById: builder.query<ProductDetail, string>({
      query: (id) => `/products/${id}`,
      transformResponse: (response: ApiResponse<ProductDetail>) => response.data || {} as ProductDetail,
    }),
    getFeaturedProducts: builder.query<Product[], number>({
      query: (count = 10) => `/products/featured?count=${count}`,
      transformResponse: (response: ApiResponse<Product[]>) => response.data || [],
    }),
  }),
});

export const {
  useGetProductsQuery,
  useGetProductBySlugQuery,
  useGetProductByIdQuery,
  useGetFeaturedProductsQuery,
} = productApi;
