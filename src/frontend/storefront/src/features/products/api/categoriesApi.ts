import type { Category, CategoryDetailDto, ApiResponse } from '@/shared/types';
import { baseApi } from '@/shared/lib/api/baseApi';

const categoriesApiSlice = baseApi.injectEndpoints({
  endpoints: (builder) => ({
    getCategories: builder.query<Category[], void>({
      query: () => '/categories',
      transformResponse: (response: ApiResponse<{ items: Category[] }>) =>
        response.data?.items || [],
      providesTags: ['Categories'],
    }),

    getTopLevelCategories: builder.query<Category[], void>({
      query: () => '/categories/top-level',
      transformResponse: (response: ApiResponse<{ items: Category[] }>) =>
        response.data?.items || [],
      providesTags: ['Categories'],
    }),

    getCategoryById: builder.query<CategoryDetailDto, string>({
      query: (id) => `/categories/${id}`,
      transformResponse: (response: ApiResponse<CategoryDetailDto>) =>
        response.data || ({} as CategoryDetailDto),
      providesTags: (_result, _error, id) => [{ type: 'Categories', id }],
    }),

    getCategoryBySlug: builder.query<CategoryDetailDto, string>({
      query: (slug) => `/categories/slug/${slug}`,
      transformResponse: (response: ApiResponse<CategoryDetailDto>) =>
        response.data || ({} as CategoryDetailDto),
      providesTags: (_result, _error, slug) => [{ type: 'Categories', id: slug }],
    }),
  }),
});

export const {
  useGetCategoriesQuery,
  useGetTopLevelCategoriesQuery,
  useGetCategoryByIdQuery,
  useGetCategoryBySlugQuery,
} = categoriesApiSlice;
