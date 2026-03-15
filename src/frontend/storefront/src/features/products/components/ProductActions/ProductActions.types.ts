export interface CartState {
  quantity: number;
  addedToCart: boolean;
  isLoading: boolean;
  error: string | null;
}

export interface WishlistState {
  isInWishlist: boolean | undefined;
  isAdding: boolean;
  isRemoving: boolean;
}

export interface ProductActionsProps {
  productId: string;
  stockQuantity: number;
  lowStockThreshold: number;
  cart: CartState;
  wishlist: WishlistState;
  onQuantityChange: (quantity: number) => void;
  onAddToCart: () => void;
  onToggleWishlist: () => void;
  onDismissError: () => void;
}
