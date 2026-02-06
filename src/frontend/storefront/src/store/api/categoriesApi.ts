import { createApi, fetchBaseQuery } from '@reduxjs/toolkit/query/react';
import { Category, CategoryDetailDto, ApiResponse } from '../../types';
import { config } from '../../config';

export const categoriesApi = createApi({
  reducerPath: 'categoriesApi',
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
    getCategories: builder.query<Category[], void>({
      query: () => '/categories',
      transformResponse: (response: ApiResponse<Category[]>) => response.data || [],
    }),

    getTopLevelCategories: builder.query<Category[], void>({
      query: () => '/categories/top-level',
      transformResponse: (response: ApiResponse<Category[]>) => response.data || [],
    }),

    getCategoryById: builder.query<CategoryDetailDto, string>({
      query: (id) => `/categories/${id}`,
      transformResponse: (response: ApiResponse<CategoryDetailDto>) =>
        response.data || ({} as CategoryDetailDto),
    }),

    getCategoryBySlug: builder.query<CategoryDetailDto, string>({
      query: (slug) => `/categories/slug/${slug}`,
      transformResponse: (response: ApiResponse<CategoryDetailDto>) =>
        response.data || ({} as CategoryDetailDto),
    }),
  }),
});

export const {
  useGetCategoriesQuery,
  useGetTopLevelCategoriesQuery,
  useGetCategoryByIdQuery,
  useGetCategoryBySlugQuery,
} = categoriesApi;
