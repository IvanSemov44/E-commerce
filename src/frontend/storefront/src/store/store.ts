import { configureStore } from '@reduxjs/toolkit';
import { authReducer } from './slices/authSlice';
import { cartReducer } from './slices/cartSlice';
import { productApi } from './api/productApi';
import { authApi } from './api/authApi';
import { ordersApi } from './api/ordersApi';
import { cartApi } from './api/cartApi';
import { categoriesApi } from './api/categoriesApi';
import { profileApi } from './api/profileApi';

export const store = configureStore({
  reducer: {
    auth: authReducer,
    cart: cartReducer,
    [productApi.reducerPath]: productApi.reducer,
    [authApi.reducerPath]: authApi.reducer,
    [ordersApi.reducerPath]: ordersApi.reducer,
    [cartApi.reducerPath]: cartApi.reducer,
    [categoriesApi.reducerPath]: categoriesApi.reducer,
    [profileApi.reducerPath]: profileApi.reducer,
  },
  middleware: (getDefaultMiddleware) =>
    getDefaultMiddleware().concat(
      productApi.middleware,
      authApi.middleware,
      ordersApi.middleware,
      cartApi.middleware,
      categoriesApi.middleware,
      profileApi.middleware
    ),
});

export type RootState = ReturnType<typeof store.getState>;
export type AppDispatch = typeof store.dispatch;
