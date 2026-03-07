import { createApi } from '@reduxjs/toolkit/query/react';
import type { ApiResponse } from '@shared/types';
import { csrfBaseQuery } from '../../utils/apiFactory';

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
  baseQuery: csrfBaseQuery,
  tagTypes: ['Review'],
  endpoints: (builder) => ({
    getPendingReviews: builder.query<ReviewDetailDto[], void>({
      query: () => '/reviews/admin/pending',
      transformResponse: (response: ApiResponse<ReviewDetailDto[]>) => response.data || [],
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

export const { useGetPendingReviewsQuery, useApproveReviewMutation, useRejectReviewMutation } =
  reviewsApi;
