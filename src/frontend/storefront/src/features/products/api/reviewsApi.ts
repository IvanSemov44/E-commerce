import type { Review, CreateReviewRequest, ApiResponse, PaginatedResult } from '@/shared/types';
import { baseApi } from '@/shared/lib/api/baseApi';

const reviewsApiSlice = baseApi.injectEndpoints({
  endpoints: (builder) => ({
    getProductReviews: builder.query<Review[], string>({
      query: (productId) => `/reviews/product/${productId}`,
      transformResponse: (response: ApiResponse<PaginatedResult<Review>>) =>
        response.data?.items || [],
      providesTags: (result) => (result ? [{ type: 'Review' as const, id: 'LIST' }] : []),
    }),

    getMyReviews: builder.query<Review[], void>({
      query: () => '/reviews/my-reviews',
      transformResponse: (response: ApiResponse<Review[]>) => response.data || [],
      providesTags: (result) => (result ? [{ type: 'Review' as const, id: 'MY_LIST' }] : []),
    }),

    createReview: builder.mutation<void, CreateReviewRequest>({
      query: (reviewData) => ({
        url: '/reviews',
        method: 'POST',
        body: reviewData,
      }),
      invalidatesTags: [{ type: 'Review' as const, id: 'LIST' }],
    }),
  }),
});

export const { useGetProductReviewsQuery, useGetMyReviewsQuery, useCreateReviewMutation } =
  reviewsApiSlice;
