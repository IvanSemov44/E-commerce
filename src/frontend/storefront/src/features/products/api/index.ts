// Product API
export {
  useGetProductsQuery,
  useGetProductBySlugQuery,
  useGetProductByIdQuery,
  useGetFeaturedProductsQuery,
} from './productApi';

export type {
  Product,
  ProductDetail,
  ProductImage,
  ProductCategory,
} from './productApi';

// Categories API
export {
  useGetCategoriesQuery,
  useGetTopLevelCategoriesQuery,
  useGetCategoryByIdQuery,
  useGetCategoryBySlugQuery,
} from './categoriesApi';

// Reviews API
export {
  useGetProductReviewsQuery,
  useGetMyReviewsQuery,
  useCreateReviewMutation,
  useUpdateReviewMutation,
  useDeleteReviewMutation,
} from './reviewsApi';
