import { Link } from 'react-router-dom';
import { useState, useEffect } from 'react';
import { useAppSelector, useAppDispatch } from '../store/hooks';
import { selectCartItems, updateQuantity, removeItem } from '../store/slices/cartSlice';
import { useGetCartQuery, useUpdateCartItemMutation, useRemoveFromCartMutation } from '../store/api/cartApi';
import { FREE_SHIPPING_THRESHOLD, STANDARD_SHIPPING_COST, DEFAULT_TAX_RATE } from '../utils/constants';
import Button from '../components/ui/Button';
import PageHeader from '../components/PageHeader';
import EmptyState from '../components/EmptyState';
import { CartSkeleton } from '../components/Skeletons';
import { CartItemList, CartSummary } from './components/Cart';
import styles from './Cart.module.css';

interface DisplayCartItem {
  id: string;
  name: string;
  slug: string;
  price: number;
  quantity: number;
  maxStock: number;
  image: string;
  compareAtPrice?: number;
  cartItemId?: string; // Backend cart item ID for updates
}

export default function Cart() {
  const dispatch = useAppDispatch();
  const { isAuthenticated } = useAppSelector((state) => state.auth);
  const localCartItems = useAppSelector(selectCartItems);
  const [displayItems, setDisplayItems] = useState<DisplayCartItem[]>(localCartItems);

  // Backend cart for authenticated users
  const { data: backendCart, isLoading: cartLoading } = useGetCartQuery(undefined, {
    skip: !isAuthenticated,
  });
  const [updateCartItem] = useUpdateCartItemMutation();
  const [removeFromCart] = useRemoveFromCartMutation();

  // Sync displayed items based on auth state
  useEffect(() => {
    if (isAuthenticated && backendCart?.items) {
      // Convert backend cart items to display format
      const convertedItems: DisplayCartItem[] = backendCart.items.map((item) => ({
        id: item.productId,
        name: item.productName,
        slug: '', // Backend doesn't provide slug
        price: item.price,
        quantity: item.quantity,
        maxStock: 999, // Backend doesn't provide stock
        image: item.imageUrl || '',
        cartItemId: item.cartItemId, // Store for backend updates
      }));
      setDisplayItems(convertedItems);
    } else {
      setDisplayItems(localCartItems);
    }
  }, [isAuthenticated, backendCart, localCartItems]);

  // Calculate cart totals
  const cartSubtotal = displayItems.reduce((sum, item) => sum + item.price * item.quantity, 0);
  const shipping = cartSubtotal > FREE_SHIPPING_THRESHOLD ? 0 : cartSubtotal > 0 ? STANDARD_SHIPPING_COST : 0;
  const tax = cartSubtotal * DEFAULT_TAX_RATE;
  const total = cartSubtotal + shipping + tax;

  const handleUpdateQuantity = async (id: string, quantity: number) => {
    if (quantity <= 0) {
      handleRemove(id);
      return;
    }

    if (isAuthenticated) {
      const item = displayItems.find((i) => i.id === id);
      if (item?.cartItemId) {
        try {
          await updateCartItem({
            cartItemId: item.cartItemId,
            quantity,
          }).unwrap();
        } catch (error) {
          console.error('Failed to update cart item:', error);
          alert('Failed to update item quantity');
        }
      }
    } else {
      dispatch(updateQuantity({ id, quantity }));
    }
  };

  const handleRemove = async (id: string) => {
    if (isAuthenticated) {
      const item = displayItems.find((i) => i.id === id);
      if (item?.cartItemId) {
        try {
          await removeFromCart(item.cartItemId).unwrap();
        } catch (error) {
          console.error('Failed to remove cart item:', error);
          alert('Failed to remove item');
        }
      }
    } else {
      dispatch(removeItem(id));
    }
  };

  const isLoading = isAuthenticated && cartLoading;

  return (
    <div className={styles.container}>
      <div className={styles.content}>
        <PageHeader title="Shopping Cart" />

        {isLoading ? (
          <CartSkeleton />
        ) : displayItems.length === 0 && !isLoading ? (
          <EmptyState
            icon={
              <svg fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M3 3h2l.4 2M7 13h10l4-8H5.4M7 13L5.4 5M7 13l-2.293 2.293c-.63.63-.184 1.707.707 1.707H17m0 0a2 2 0 100 4 2 2 0 000-4zm-8 2a2 2 0 11-4 0 2 2 0 014 0z"
                />
              </svg>
            }
            title="Your cart is empty"
            action={
              <Link to="/products">
                <Button size="lg">Continue Shopping</Button>
              </Link>
            }
          />
        ) : (
          <div className={styles.grid}>
            <CartItemList
              items={displayItems}
              onUpdateQuantity={handleUpdateQuantity}
              onRemove={handleRemove}
            />
            <CartSummary
              subtotal={cartSubtotal}
              shipping={shipping}
              tax={tax}
              total={total}
              freeShippingThreshold={FREE_SHIPPING_THRESHOLD}
            />
          </div>
        )}
      </div>
    </div>
  );
}
