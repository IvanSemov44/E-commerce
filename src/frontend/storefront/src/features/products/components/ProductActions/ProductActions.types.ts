export interface CartItem {
  quantity: number;
}

export interface ProductActionsProps {
  stockQuantity: number;
  lowStockThreshold: number;
  quantity: number;
  cartItem: CartItem | undefined;
  addedToCart: boolean;
  addingToCartBackend: boolean;
  cartError: string | null;
  isAuthenticated: boolean;
  isInWishlist: boolean | undefined;
  addingToWishlist: boolean;
  removingFromWishlist: boolean;
  onQuantityChange: (quantity: number) => void;
  onAddToCart: () => void;
  onToggleWishlist: () => void;
  onDismissError: () => void;
}
