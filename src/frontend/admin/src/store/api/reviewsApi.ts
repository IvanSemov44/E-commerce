import { createApi, fetchBaseQuery } from '@reduxjs/toolkit/query/react';
import type { ApiResponse } from '@shared/types';

const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000/api';

export interface ReviewDetailDto {
  id: string;
  productId: string;
  productName?: string;
  title?: string;
  comment?: string;
  rating: number;
  userName?: string;
  isVerified: boolean;
  isApproved: boolean;
  createdAt: string;
  updatedAt: string;
}

export const reviewsApi = createApi({
  reducerPath: 'reviewsApi',
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
  tagTypes: ['Review'],
  endpoints: (builder) => ({
    getPendingReviews: builder.query<ReviewDetailDto[], void>({
      query: () => '/reviews/admin/pending',
      transformResponse: (response: ApiResponse<ReviewDetailDto[]>) =>
        response.data || [],
      providesTags: ['Review'],
    }),
    approveReview: builder.mutation<void, string>({
      query: (reviewId) => ({
        url: `/reviews/${reviewId}/approve`,
        method: 'POST',
      }),
      invalidatesTags: ['Review'],
    }),
    rejectReview: builder.mutation<void, string>({
      query: (reviewId) => ({
        url: `/reviews/${reviewId}/reject`,
        method: 'POST',
      }),
      invalidatesTags: ['Review'],
    }),
  }),
});

export const {
  useGetPendingReviewsQuery,
  useApproveReviewMutation,
  useRejectReviewMutation,
} = reviewsApi;
