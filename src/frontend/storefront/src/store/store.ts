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

export const store = configureStore({
  reducer: {
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
  },
  middleware: (getDefaultMiddleware) =>
    getDefaultMiddleware().concat(
      productApi.middleware,
      authApi.middleware,
      ordersApi.middleware,
      cartApi.middleware,
      categoriesApi.middleware,
      profileApi.middleware,
      reviewsApi.middleware,
      wishlistApi.middleware
    ),
});

export type RootState = ReturnType<typeof store.getState>;
export type AppDispatch = typeof store.dispatch;
