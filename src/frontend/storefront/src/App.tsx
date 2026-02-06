import { BrowserRouter as Router, Routes, Route } from 'react-router-dom';
import { useEffect } from 'react';
import { useAppDispatch, useAppSelector } from './store/hooks';
import { setUser } from './store/slices/authSlice';
import { useGetProfileQuery } from './store/api/profileApi';
import { useCartSync, useErrorHandler } from './hooks';
import ToastContainer from './components/Toast/ToastContainer';
import ErrorBoundary from './components/ErrorBoundary';

// Pages
import Home from './pages/Home';
import Products from './pages/Products';
import ProductDetail from './pages/ProductDetail';
import Cart from './pages/Cart';
import Checkout from './pages/Checkout';
import OrderHistory from './pages/OrderHistory';
import OrderDetail from './pages/OrderDetail';
import Profile from './pages/Profile';
import Wishlist from './pages/Wishlist';
import Login from './pages/Login';
import Register from './pages/Register';
import ForgotPassword from './pages/ForgotPassword';
import ResetPassword from './pages/ResetPassword';

// Components
import Header from './components/Header';
import Footer from './components/Footer';
import ProtectedRoute from './components/ProtectedRoute';

function AppContent() {
  const dispatch = useAppDispatch();
  const { isAuthenticated, token } = useAppSelector((state) => state.auth);
  const { user } = useAppSelector((state) => state.auth);
  const { handleError, clearError } = useErrorHandler();

  // Fetch user profile when authenticated
  const {
    data: profileData,
    error: profileError,
    isLoading: profileLoading,
  } = useGetProfileQuery(undefined, {
    skip: !isAuthenticated || !!user || !token,
  });

  // Use cart sync hook to handle local-to-backend cart sync
  const { isLoading: cartLoading } = useCartSync({
    enabled: isAuthenticated && !!token,
  });

  /**
   * Update Redux auth state when profile is fetched
   */
  useEffect(() => {
    if (profileData && !user) {
      dispatch(
        setUser({
          ...profileData,
          role: profileData.role || 'customer',
        })
      );
      clearError();
    }
  }, [profileData, user, dispatch, clearError]);

  /**
   * Handle profile fetch errors
   */
  useEffect(() => {
    if (profileError) {
      console.error('Failed to load user profile:', profileError);
      handleError(profileError);

      // If 401 Unauthorized, user's token is invalid - logout will be handled by API middleware
      // For other errors, we continue with null user and let app gracefully degrade
    }
  }, [profileError, handleError]);

  // Show loading state while fetching profile
  const isAppLoading = profileLoading || cartLoading;

  return (
    <div>
      <Header />
      <main>
        {isAppLoading && (
          <div style={{ textAlign: 'center', padding: '2rem' }}>
            Loading...
          </div>
        )}
        {!isAppLoading && (
          <Routes>
            <Route path="/" element={<Home />} />
            <Route path="/products" element={<Products />} />
            <Route path="/products/:slug" element={<ProductDetail />} />
            <Route path="/cart" element={<Cart />} />
            <Route path="/checkout" element={<Checkout />} />
            <Route
              path="/wishlist"
              element={
                <ProtectedRoute>
                  <Wishlist />
                </ProtectedRoute>
              }
            />
            <Route
              path="/orders"
              element={
                <ProtectedRoute>
                  <OrderHistory />
                </ProtectedRoute>
              }
            />
            <Route
              path="/orders/:orderId"
              element={
                <ProtectedRoute>
                  <OrderDetail />
                </ProtectedRoute>
              }
            />
            <Route
              path="/profile"
              element={
                <ProtectedRoute>
                  <Profile />
                </ProtectedRoute>
              }
            />
            <Route path="/login" element={<Login />} />
            <Route path="/register" element={<Register />} />
            <Route path="/forgot-password" element={<ForgotPassword />} />
            <Route path="/reset-password" element={<ResetPassword />} />
          </Routes>
        )}
      </main>
      <Footer />
    </div>
  );
}

function App() {
  return (
    <Router>
      <ErrorBoundary>
        <ToastContainer />
        <AppContent />
      </ErrorBoundary>
    </Router>
  );
}

export default App;
