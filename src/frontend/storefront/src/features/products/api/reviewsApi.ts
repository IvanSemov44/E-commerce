import type { Review, CreateReviewRequest, UpdateReviewRequest, ApiResponse } from '@/shared/types';
import { baseApi } from '@/shared/lib/api/baseApi';

// Use ProductReview as alias for Review (API convention)
type ProductReview = Review;

const reviewsApiSlice = baseApi.injectEndpoints({
  endpoints: (builder) => ({
    getProductReviews: builder.query<ProductReview[], string>({
      query: (productId) => `/reviews/product/${productId}`,
      transformResponse: (response: ApiResponse<ProductReview[]>) => response.data || [],
      providesTags: (result) => (result ? [{ type: 'Review' as const, id: 'LIST' }] : []),
    }),

    getMyReviews: builder.query<ProductReview[], void>({
      query: () => '/reviews/my-reviews',
      transformResponse: (response: ApiResponse<ProductReview[]>) => response.data || [],
      providesTags: (result) => (result ? [{ type: 'Review' as const, id: 'MY_LIST' }] : []),
    }),

    createReview: builder.mutation<ProductReview, CreateReviewRequest>({
      query: (reviewData) => ({
        url: '/reviews',
        method: 'POST',
        body: reviewData,
      }),
      transformResponse: (response: ApiResponse<ProductReview>) =>
        response.data || ({} as ProductReview),
      invalidatesTags: [{ type: 'Review' as const, id: 'LIST' }],
    }),

    updateReview: builder.mutation<ProductReview, { reviewId: string; data: UpdateReviewRequest }>({
      query: ({ reviewId, data }) => ({
        url: `/reviews/${reviewId}`,
        method: 'PUT',
        body: data,
      }),
      transformResponse: (response: ApiResponse<ProductReview>) =>
        response.data || ({} as ProductReview),
      invalidatesTags: [{ type: 'Review' as const, id: 'LIST' }],
    }),

    deleteReview: builder.mutation<void, string>({
      query: (reviewId) => ({
        url: `/reviews/${reviewId}`,
        method: 'DELETE',
      }),
      // Keep a consistent API envelope contract, even for empty payload responses.
      transformResponse: () => undefined,
      invalidatesTags: [{ type: 'Review' as const, id: 'LIST' }],
    }),
  }),
});

export const {
  useGetProductReviewsQuery,
  useGetMyReviewsQuery,
  useCreateReviewMutation,
  useUpdateReviewMutation,
  useDeleteReviewMutation,
} = reviewsApiSlice;
