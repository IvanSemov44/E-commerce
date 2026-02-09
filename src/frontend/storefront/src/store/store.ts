import { configureStore } from '@reduxjs/toolkit';
import { authReducer } from './slices/authSlice';
import { cartReducer } from './slices/cartSlice';
import toastReducer from './slices/toastSlice';
import { productApi } from './api/productApi';
import { authApi } from './api/authApi';
import { ordersApi } from './api/ordersApi';
import { cartApi } from './api/cartApi';
import { categoriesApi } from './api/categoriesApi';
import { profileApi } from './api/profileApi';
import { reviewsApi } from './api/reviewsApi';
import { wishlistApi } from './api/wishlistApi';
import { promoCodeApi } from './api/promoCodeApi';
import { inventoryApi } from './api/inventoryApi';
import { cartPersistenceMiddleware } from './middleware/cartPersistence';

const rootReducer = {
  auth: authReducer,
  cart: cartReducer,
  toast: toastReducer,
  [productApi.reducerPath]: productApi.reducer,
  [authApi.reducerPath]: authApi.reducer,
  [ordersApi.reducerPath]: ordersApi.reducer,
  [cartApi.reducerPath]: cartApi.reducer,
  [categoriesApi.reducerPath]: categoriesApi.reducer,
  [profileApi.reducerPath]: profileApi.reducer,
  [reviewsApi.reducerPath]: reviewsApi.reducer,
  [wishlistApi.reducerPath]: wishlistApi.reducer,
  [promoCodeApi.reducerPath]: promoCodeApi.reducer,
  [inventoryApi.reducerPath]: inventoryApi.reducer,
};

export const store = configureStore({
  reducer: rootReducer,
  middleware: (getDefaultMiddleware) =>
    getDefaultMiddleware().concat(
      productApi.middleware,
      authApi.middleware,
      ordersApi.middleware,
      cartApi.middleware,
      categoriesApi.middleware,
      profileApi.middleware,
      reviewsApi.middleware,
      wishlistApi.middleware,
      promoCodeApi.middleware,
      inventoryApi.middleware,
      cartPersistenceMiddleware
    ),
});

export type RootState = ReturnType<typeof store.getState>;
export type AppDispatch = typeof store.dispatch;
