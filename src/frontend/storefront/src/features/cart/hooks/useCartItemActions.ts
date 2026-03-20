import { useCallback } from 'react';
import { useAppSelector, useAppDispatch } from '@/shared/lib/store';
import { updateQuantity, removeItem } from '../slices/cartSlice';
import {
  useGetCartQuery,
  useUpdateCartItemMutation,
  useRemoveFromCartMutation,
} from '../api/cartApi';
import { useToast } from '@/shared/hooks';

export function useCartItemActions(itemId: string) {
  const dispatch = useAppDispatch();
  const { success, error: showError } = useToast();
  const isAuthenticated = useAppSelector((state) => state.auth.isAuthenticated);

  const { data: backendCart } = useGetCartQuery(undefined, { skip: !isAuthenticated });
  const [updateCartItem] = useUpdateCartItemMutation();
  const [removeFromCartApi] = useRemoveFromCartMutation();

  const handleUpdateQuantity = useCallback(
    async (quantity: number) => {
      if (quantity < 1) return;
      try {
        if (isAuthenticated) {
          await updateCartItem({ cartItemId: itemId, quantity }).unwrap();
        } else {
          dispatch(updateQuantity({ id: itemId, quantity }));
        }
        success('Cart updated');
      } catch {
        showError('Failed to update cart');
      }
    },
    [isAuthenticated, itemId, updateCartItem, dispatch, success, showError]
  );

  const handleRemove = useCallback(async () => {
    try {
      if (isAuthenticated) {
        const cartItem = backendCart?.items.find((item) => item.id === itemId);
        const productId = cartItem?.productId;
        await removeFromCartApi(itemId).unwrap();
        if (productId) dispatch(removeItem(productId));
      } else {
        dispatch(removeItem(itemId));
      }
      success('Item removed from cart');
    } catch {
      showError('Failed to remove item');
    }
  }, [isAuthenticated, itemId, backendCart, removeFromCartApi, dispatch, success, showError]);

  return { handleUpdateQuantity, handleRemove };
}
