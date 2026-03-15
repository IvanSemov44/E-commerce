/**
 * ProductCard Types
 * Type definitions for the ProductCard component
 */

export interface ProductCardProps {
  id: string;
  name: string;
  slug: string;
  price: number;
  compareAtPrice?: number;
  imageUrl: string;
  rating?: number;
  reviewCount?: number;
  stockQuantity?: number;
}
