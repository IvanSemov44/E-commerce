interface CartItem {
  quantity: number;
}

export interface CartState {
  quantity: number;
  cartItem: CartItem | undefined;
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
  stockQuantity: number;
  lowStockThreshold: number;
  isAuthenticated: boolean;
  cart: CartState;
  wishlist: WishlistState;
  onQuantityChange: (quantity: number) => void;
  onAddToCart: () => void;
  onToggleWishlist: () => void;
  onDismissError: () => void;
}
