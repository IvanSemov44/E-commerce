/**
 * Product Types
 * Centralized type definitions for the Products feature
 */

import type { ApiResponse, PaginatedResult, Product, ProductDetail } from '@/shared/types';

export interface GetProductsQueryParams {
  page?: number;
  pageSize?: number;
  categoryId?: string;
  search?: string;
  minPrice?: number;
  maxPrice?: number;
  minRating?: number;
  isFeatured?: boolean;
  sortBy?: string;
  sortOrder?: string;
}

export type GetProductsResponse = ApiResponse<PaginatedResult<Product>>;

export type GetProductBySlugResponse = ApiResponse<ProductDetail>;

export type GetProductByIdResponse = ApiResponse<ProductDetail>;

export type GetFeaturedProductsResponse = ApiResponse<Product[]>;
