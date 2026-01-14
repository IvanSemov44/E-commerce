import { createApi, fetchBaseQuery } from '@reduxjs/toolkit/query/react';

const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000/api';

export interface ProductImage {
  id: string;
  url: string;
  altText?: string;
  isPrimary: boolean;
}

export interface ProductCategory {
  id: string;
  name: string;
  slug: string;
  imageUrl?: string;
}

export interface ProductReview {
  id: string;
  title?: string;
  comment?: string;
  rating: number;
  userName?: string;
  createdAt: string;
}

export interface Product {
  id: string;
  name: string;
  slug: string;
  shortDescription?: string;
  price: number;
  compareAtPrice?: number;
  stockQuantity: number;
  isFeatured: boolean;
  images: ProductImage[];
  category?: ProductCategory;
  averageRating: number;
  reviewCount: number;
}

export interface ProductDetail extends Product {
  description?: string;
  sku?: string;
  lowStockThreshold: number;
  isActive: boolean;
  reviews: ProductReview[];
}

export interface PaginatedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface ApiResponse<T> {
  success: boolean;
  message: string;
  data?: T;
  errors?: string[];
}

export const productApi = createApi({
  reducerPath: 'productApi',
  baseQuery: fetchBaseQuery({
    baseUrl: API_URL,
    prepareHeaders: (headers) => {
      const token = localStorage.getItem('authToken');
      if (token) {
        headers.set('Authorization', `Bearer ${token}`);
      }
      return headers;
    },
  }),
  endpoints: (builder) => ({
    getProducts: builder.query<PaginatedResult<Product>, { page?: number; pageSize?: number }>({
      query: ({ page = 1, pageSize = 20 }) =>
        `/products?page=${page}&pageSize=${pageSize}`,
    }),
    getProductBySlug: builder.query<ProductDetail, string>({
      query: (slug) => `/products/slug/${slug}`,
    }),
    getProductById: builder.query<ProductDetail, string>({
      query: (id) => `/products/${id}`,
    }),
    getFeaturedProducts: builder.query<Product[], number>({
      query: (count = 10) => `/products/featured?count=${count}`,
    }),
  }),
});

export const {
  useGetProductsQuery,
  useGetProductBySlugQuery,
  useGetProductByIdQuery,
  useGetFeaturedProductsQuery,
} = productApi;
