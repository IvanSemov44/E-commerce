import { createApi, fetchBaseQuery } from '@reduxjs/toolkit/query/react';
import type { ApiResponse } from '@shared/types';

const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000/api';

/**
 * Helper function to get CSRF token from cookie
 */
const getCsrfToken = (): string | null => {
  if (typeof document === 'undefined') return null;
  const match = document.cookie.match(/XSRF-TOKEN=([^;]+)/);
  return match ? decodeURIComponent(match[1]) : null;
};

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
