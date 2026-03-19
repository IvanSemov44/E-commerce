import { useTranslation } from 'react-i18next';
import { useAppSelector } from '@/shared/lib/store';
import { useApiErrorHandler, useToast } from '@/shared/hooks';
import {
  useGetWishlistQuery,
  useAddToWishlistMutation,
  useRemoveFromWishlistMutation,
} from '@/features/wishlist/api';

export function useWishlistToggle(productId: string | undefined) {
  const { t } = useTranslation();
  const { isAuthenticated } = useAppSelector((state) => state.auth);
  const { data: wishlist } = useGetWishlistQuery(undefined, { skip: !isAuthenticated });
  const [addToWishlist, { isLoading: isAdding }] = useAddToWishlistMutation();
  const [removeFromWishlist, { isLoading: isRemoving }] = useRemoveFromWishlistMutation();
  const { handleError } = useApiErrorHandler();
  const { success } = useToast();

  const isInWishlist = wishlist?.items.some((item) => item.productId === productId) ?? false;
  const isWishlistLoading = isAdding || isRemoving;

  const toggleWishlist = async () => {
    if (!productId || !isAuthenticated) return;
    try {
      if (isInWishlist) {
        await removeFromWishlist(productId).unwrap();
        success(t('wishlist.removedFromWishlist'));
      } else {
        await addToWishlist(productId).unwrap();
        success(t('wishlist.addedToWishlist'));
      }
    } catch (err) {
      handleError(err, t('wishlist.updateError'));
    }
  };

  return { isInWishlist, toggleWishlist, isWishlistLoading, isAdding, isRemoving };
}
