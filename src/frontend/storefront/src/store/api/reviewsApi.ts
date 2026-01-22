import { createApi, fetchBaseQuery } from '@reduxjs/toolkit/query/react';

const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000/api';

export interface ProductReview {
  id: string;
  title?: string;
  comment?: string;
  rating: number;
  userName?: string;
  createdAt: string;
}

export interface CreateReviewRequest {
  productId: string;
  title?: string;
  comment: string;
  rating: number;
}

export interface UpdateReviewRequest {
  title?: string;
  comment: string;
  rating: number;
}

export interface ApiResponse<T> {
  success: boolean;
  message: string;
  data?: T;
  errors?: string[];
}

export const reviewsApi = createApi({
  reducerPath: 'reviewsApi',
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
  tagTypes: ['Review'],
  endpoints: (builder) => ({
    getProductReviews: builder.query<ProductReview[], string>({
      query: (productId) => `/reviews/product/${productId}`,
      transformResponse: (response: ApiResponse<ProductReview[]>) => response.data || [],
      providesTags: ['Review'],
    }),

    getMyReviews: builder.query<ProductReview[], void>({
      query: () => '/reviews/my-reviews',
      transformResponse: (response: ApiResponse<ProductReview[]>) => response.data || [],
      providesTags: ['Review'],
    }),

    createReview: builder.mutation<ProductReview, CreateReviewRequest>({
      query: (reviewData) => ({
        url: '/reviews',
        method: 'POST',
        body: reviewData,
      }),
      transformResponse: (response: ApiResponse<ProductReview>) => response.data || {} as ProductReview,
      invalidatesTags: ['Review'],
    }),

    updateReview: builder.mutation<ProductReview, { reviewId: string; data: UpdateReviewRequest }>({
      query: ({ reviewId, data }) => ({
        url: `/reviews/${reviewId}`,
        method: 'PUT',
        body: data,
      }),
      transformResponse: (response: ApiResponse<ProductReview>) => response.data || {} as ProductReview,
      invalidatesTags: ['Review'],
    }),

    deleteReview: builder.mutation<void, string>({
      query: (reviewId) => ({
        url: `/reviews/${reviewId}`,
        method: 'DELETE',
      }),
      invalidatesTags: ['Review'],
    }),
  }),
});

export const {
  useGetProductReviewsQuery,
  useGetMyReviewsQuery,
  useCreateReviewMutation,
  useUpdateReviewMutation,
  useDeleteReviewMutation,
} = reviewsApi;
