import { configureStore } from '@reduxjs/toolkit';
import { authReducer } from '@/features/auth/slices/authSlice';
import { cartReducer } from '@/features/cart/slices/cartSlice';
import { toastReducer } from '@/app/Toast/toastSlice';
import { baseApi } from '@/shared/lib/api/baseApi';
import { cartPersistenceMiddleware } from './middleware/cartPersistence';

// Import API slices so endpoints get injected
import '@/features/auth/api/authApi';
import '@/features/cart/api/cartApi';
import '@/features/orders/api/ordersApi';
import '@/features/products/api/productApi';
import '@/features/products/api/categoriesApi';
import '@/features/products/api/reviewsApi';
import '@/features/profile/api/profileApi';
import '@/features/wishlist/api/wishlistApi';
import '@/features/checkout/api/inventoryApi';
import '@/features/checkout/api/promoCodeApi';

const rootReducer = {
  auth: authReducer,
  cart: cartReducer,
  toast: toastReducer,
  [baseApi.reducerPath]: baseApi.reducer,
};

export const store = configureStore({
  reducer: rootReducer,
  middleware: (getDefaultMiddleware) =>
    getDefaultMiddleware().concat(baseApi.middleware, cartPersistenceMiddleware),
});

export type RootState = ReturnType<typeof store.getState>;
export type AppDispatch = typeof store.dispatch;
