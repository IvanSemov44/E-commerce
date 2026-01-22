import { createApi, fetchBaseQuery } from '@reduxjs/toolkit/query/react';

const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000/api';

export interface Category {
  id: string;
  name: string;
  slug: string;
  description?: string;
  imageUrl?: string;
  parentCategoryId?: string;
  subcategories?: Category[];
}

export interface CategoryDetailDto {
  id: string;
  name: string;
  slug: string;
  description?: string;
  imageUrl?: string;
  parentCategoryId?: string;
  parent?: Category;
  children?: Category[];
  productCount?: number;
}

export interface ApiResponse<T> {
  success: boolean;
  message: string;
  data?: T;
  errors?: string[];
}

export const categoriesApi = createApi({
  reducerPath: 'categoriesApi',
  baseQuery: fetchBaseQuery({
    baseUrl: API_URL,
    prepareHeaders: (headers) => {
      if (typeof window !== 'undefined') {
        const token = localStorage.getItem('authToken');
        if (token) {
          headers.set('Authorization', `Bearer ${token}`);
        }
      }
      return headers;
    },
  }),
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
