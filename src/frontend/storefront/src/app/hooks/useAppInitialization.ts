import { useAppSelector } from '@/shared/lib/store';
import { useCartSync } from '@/features/cart/hooks';
import { selectIsAuthenticated } from '@/features/auth/slices/authSlice';
import { useAuthBootstrap } from './useAuthBootstrap';

interface UseAppInitializationResult {
  isInitializing: boolean;
}

export function useAppInitialization(): UseAppInitializationResult {
  const { initialized, isCurrentUserLoading } = useAuthBootstrap();
  const isAuthenticated = useAppSelector(selectIsAuthenticated);

  const { isLoading: cartLoading } = useCartSync({
    enabled: initialized && isAuthenticated,
  });

  return {
    isInitializing: !initialized || isCurrentUserLoading || cartLoading,
  };
}
