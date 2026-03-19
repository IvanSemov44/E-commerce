import type { Product, ProductDetail, PaginatedResult, ApiResponse } from '@/shared/types';
import { baseApi } from '@/shared/lib/api/baseApi';
import {
  DEFAULT_PRODUCTS_PAGE_SIZE,
  FEATURED_PRODUCTS_PAGE_SIZE,
} from '@/features/products/constants';
import type { SortBy } from '@/features/products/constants';

interface GetProductsQueryParams {
  page?: number;
  pageSize?: number;
  categoryId?: string;
  search?: string;
  minPrice?: number;
  maxPrice?: number;
  minRating?: number;
  isFeatured?: boolean;
  sortBy?: SortBy;
}

const productApiSlice = baseApi.injectEndpoints({
  endpoints: (builder) => ({
    getProducts: builder.query<PaginatedResult<Product>, GetProductsQueryParams>({
      query: ({
        page = 1,
        pageSize = DEFAULT_PRODUCTS_PAGE_SIZE,
        categoryId,
        search,
        minPrice,
        maxPrice,
        minRating,
        isFeatured,
        sortBy,
      }) => {
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
        response.data || {
          items: [],
          totalCount: 0,
          page: 1,
          pageSize: DEFAULT_PRODUCTS_PAGE_SIZE,
          totalPages: 0,
          hasNext: false,
          hasPrevious: false,
        },
      providesTags: ['Products'],
    }),
    getProductBySlug: builder.query<ProductDetail, string>({
      query: (slug) => `/products/slug/${slug}`,
      transformResponse: (response: ApiResponse<ProductDetail>) =>
        response.data || ({} as ProductDetail),
      providesTags: ['Products'],
    }),
    getProductById: builder.query<ProductDetail, string>({
      query: (id) => `/products/${id}`,
      transformResponse: (response: ApiResponse<ProductDetail>) =>
        response.data || ({} as ProductDetail),
      providesTags: ['Products'],
    }),
    getFeaturedProducts: builder.query<
      PaginatedResult<Product>,
      { page?: number; pageSize?: number } | void
    >({
      query: (args) => {
        const params = new URLSearchParams();
        params.set('page', (args?.page ?? 1).toString());
        params.set('pageSize', (args?.pageSize ?? FEATURED_PRODUCTS_PAGE_SIZE).toString());
        return `/products/featured?${params}`;
      },
      transformResponse: (response: ApiResponse<PaginatedResult<Product>>) =>
        response.data || {
          items: [],
          totalCount: 0,
          page: 1,
          pageSize: FEATURED_PRODUCTS_PAGE_SIZE,
          totalPages: 0,
          hasNext: false,
          hasPrevious: false,
        },
      providesTags: ['Products'],
    }),
  }),
});

export const {
  useGetProductsQuery,
  useGetProductBySlugQuery,
  useGetProductByIdQuery,
  useGetFeaturedProductsQuery,
} = productApiSlice;
