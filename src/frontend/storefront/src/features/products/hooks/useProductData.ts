import { useGetProductBySlugQuery, useGetProductReviewsQuery } from '@/features/products/api';

export function useProductData(slug: string) {
  const { data: product, isLoading, error } = useGetProductBySlugQuery(slug);

  const {
    data: reviews,
    isLoading: reviewsLoading,
    error: reviewsError,
    refetch: refetchReviews,
  } = useGetProductReviewsQuery(product?.id ?? '', { skip: !product?.id });

  return { product, isLoading, error, reviews, reviewsLoading, reviewsError, refetchReviews };
}
