import { configureStore } from '@reduxjs/toolkit';
import { authReducer } from './slices/authSlice';
import { cartReducer } from './slices/cartSlice';
import { languageReducer } from './slices/languageSlice';
import toastReducer from './slices/toastSlice';
import { baseApi } from './api/baseApi';
import { cartPersistenceMiddleware } from './middleware/cartPersistence';

// Import API slices so endpoints get injected
import './api/authApi';
import './api/cartApi';
import './api/ordersApi';
import './api/productApi';
import './api/profileApi';
import './api/reviewsApi';
import './api/wishlistApi';
import './api/categoriesApi';
import './api/promoCodeApi';
import './api/inventoryApi';

const rootReducer = {
  auth: authReducer,
  cart: cartReducer,
  language: languageReducer,
  toast: toastReducer,
  [baseApi.reducerPath]: baseApi.reducer,
};

export const store = configureStore({
  reducer: rootReducer,
  middleware: (getDefaultMiddleware) =>
    getDefaultMiddleware().concat(
      baseApi.middleware,
      cartPersistenceMiddleware
    ),
});

export type RootState = ReturnType<typeof store.getState>;
export type AppDispatch = typeof store.dispatch;
