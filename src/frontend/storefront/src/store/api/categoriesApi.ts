import type { Category, CategoryDetailDto, ApiResponse } from '../../types';
import { baseApi } from './baseApi';

const categoriesApiSlice = baseApi.injectEndpoints({
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
} = categoriesApiSlice;
