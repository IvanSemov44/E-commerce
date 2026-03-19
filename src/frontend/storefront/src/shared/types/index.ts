/**
 * Centralized type definitions for the entire application
 * This file serves as the single source of truth for all API interfaces
 */

// ============================================
// Auth Types
// ============================================

export interface AuthUser {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  phone?: string;
  role: string;
  avatarUrl?: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  email: string;
  password: string;
  firstName: string;
  lastName: string;
}

export interface ForgotPasswordRequest {
  email: string;
}

export interface ResetPasswordRequest {
  email: string;
  token: string;
  newPassword: string;
}

export interface AuthResponse {
  success: boolean;
  message: string;
  user?: AuthUser;
  token?: string;
}

// ============================================
// Cart Types
// ============================================

export interface CartItemDto {
  id: string; // Cart item ID (renamed from cartItemId to match backend)
  productId: string;
  productName: string;
  quantity: number;
  price: number;
  imageUrl?: string;
  productImage?: string; // Backend returns this field
}

export interface CartDto {
  id: string;
  items: CartItemDto[];
  subtotal: number;
  itemCount: number;
}

export interface AddToCartRequest {
  productId: string;
  quantity: number;
}

export interface UpdateCartItemRequest {
  cartItemId: string;
  quantity: number;
}

// ============================================
// Profile Types
// ============================================

export interface UserProfile {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  phone?: string;
  avatarUrl?: string;
  role?: string;
}

export interface UpdateProfileRequest {
  firstName?: string;
  lastName?: string;
  phone?: string;
  avatarUrl?: string;
}

// ============================================
// Generic API Response
// ============================================

export interface ApiResponse<T> {
  success: boolean;
  message: string;
  data?: T;
  errors?: string[];
}

// ============================================
// Product Types
// ============================================

export interface ProductImage {
  id: string;
  url: string;
  altText?: string;
  isPrimary: boolean;
}

export interface ProductCategory {
  id: string;
  name: string;
  slug: string;
  imageUrl?: string;
}

export interface ProductReview {
  id: string;
  title?: string;
  comment?: string;
  rating: number;
  userName?: string;
  createdAt: string;
}

export interface Product {
  id: string;
  name: string;
  slug: string;
  price: number;
  compareAtPrice?: number;
  description?: string;
  shortDescription?: string;
  images: ProductImage[];
  stockQuantity: number;
  averageRating: number;
  reviewCount: number;
  isFeatured?: boolean;
  category?: ProductCategory;
}

export interface ProductDetail extends Product {
  sku?: string;
  lowStockThreshold: number;
  isActive: boolean;
  reviews: ProductReview[];
}

export interface PaginatedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasNext: boolean;
  hasPrevious: boolean;
}

// ============================================
// Order Types
// ============================================

export interface OrderItem {
  id?: string;
  productId?: string;
  productName: string;
  productSku?: string;
  productImageUrl?: string;
  quantity: number;
  unitPrice: number;
  totalPrice: number;
}

export interface Address {
  firstName: string;
  lastName: string;
  phone: string;
  streetLine1: string;
  streetLine2?: string;
  city: string;
  state: string;
  postalCode: string;
  country: string;
}

export interface CreateOrderItemRequest {
  productId: string;
  quantity: number;
}

export interface CreateOrderRequest {
  items: CreateOrderItemRequest[];
  shippingAddress: Address;
  billingAddress?: Address;
  paymentMethod: string;
  promoCode?: string;
  guestEmail?: string;
}

export interface OrderResponse {
  orderId: string;
  orderNumber: string;
  clientSecret?: string;
  totals: {
    subtotal: number;
    discount?: number;
    shipping: number;
    tax: number;
    total: number;
  };
}

export interface Order {
  id: string;
  orderNumber: string;
  status: 'Pending' | 'Processing' | 'Shipped' | 'Delivered' | 'Cancelled';
  createdAt: string;
  items: OrderItem[];
  subtotal: number;
  discountAmount?: number;
  shippingAmount: number;
  taxAmount: number;
  totalAmount: number;
  shippingAddress: Address;
  notes?: string;
  trackingNumber?: string;
}

// ============================================
// Review Types
// ============================================

export interface Review {
  id: string;
  productId?: string;
  userId?: string;
  title?: string;
  comment?: string;
  rating: number;
  userName?: string;
  createdAt: string;
  helpful?: number;
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

// ============================================
// Category Types
// ============================================

export interface Category {
  id: string;
  name: string;
  slug: string;
  description?: string;
  parentCategoryId?: string;
  imageUrl?: string;
  subcategories?: Category[];
}

export interface CategoryDetailDto {
  id: string;
  name: string;
  slug: string;
  description?: string;
  imageUrl?: string;
  parentCategoryId?: string;
  parent?: Category;
  children?: Category[];
  productCount?: number;
}

// ============================================
// Error Types
// ============================================

export interface ApiError {
  message: string;
  status?: number;
  errors?: Record<string, string[]>;
}

// ============================================
// Local Cart State Types (Redux)
// ============================================

export interface LocalCartItem {
  id: string;
  name: string;
  price: number;
  compareAtPrice?: number;
  quantity: number;
  imageUrl?: string;
}

// ============================================
// Auth State Types (Redux)
// ============================================

export interface AuthState {
  isAuthenticated: boolean;
  user: AuthUser | null;
  token: string | null;
  loading: boolean;
  error: string | null;
}
