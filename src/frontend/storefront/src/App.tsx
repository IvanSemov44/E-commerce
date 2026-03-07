import { BrowserRouter as Router, Routes, Route, useLocation } from 'react-router-dom';
import { useEffect, lazy, Suspense } from 'react';
import { Toaster } from 'react-hot-toast';
import { useAppDispatch, useAppSelector } from '@/shared/lib/store';
import { setUser } from '@/features/auth/slices/authSlice';
import { useGetProfileQuery } from './features/profile/api/profileApi';
import { useCartSync } from './features/cart/hooks';
import { useErrorHandler } from '@/shared/hooks/useErrorHandler';
import { logger } from '@/shared/lib/utils/logger';
import { telemetry } from '@/shared/lib/utils/telemetry';
import ToastContainer from './shared/components/Toast/ToastContainer';
import ErrorBoundary from './shared/components/ErrorBoundary';

// Eagerly load critical pages (home, login, register)
import Home from './pages/HomePage/HomePage';
import Login from './features/auth/pages/LoginPage';
import Register from './features/auth/pages/RegisterPage';

// Lazy load non-critical pages for code splitting
const Products = lazy(() => import('./features/products/pages/ProductsPage/ProductsPage'));
const ProductDetail = lazy(() => import('./features/products/pages/ProductDetailPage/ProductDetailPage'));
const Cart = lazy(() => import('./features/cart/pages/CartPage/CartPage'));
const Checkout = lazy(() => import('./pages/CheckoutPage/CheckoutPage'));
const OrderHistory = lazy(() => import('./features/orders/pages/OrderHistoryPage/OrderHistoryPage'));
const OrderDetail = lazy(() => import('./features/orders/pages/OrderDetailPage/OrderDetailPage'));
const Profile = lazy(() => import('./features/profile/pages/ProfilePage/ProfilePage'));
const Wishlist = lazy(() => import('./features/wishlist/pages/WishlistPage/WishlistPage'));
const ForgotPassword = lazy(() => import('./features/auth/pages/ForgotPasswordPage'));
const ResetPassword = lazy(() => import('./features/auth/pages/ResetPasswordPage'));

// Content pages
const PrivacyPolicy = lazy(() => import('./pages/PrivacyPolicy'));
const TermsOfService = lazy(() => import('./pages/TermsOfService'));
const ReturnsPolicy = lazy(() => import('./pages/ReturnsPolicy'));
const CookiePolicy = lazy(() => import('./pages/CookiePolicy'));
const Security = lazy(() => import('./pages/Security'));
const AboutUs = lazy(() => import('./pages/AboutUs'));
const Careers = lazy(() => import('./pages/Careers'));
const Press = lazy(() => import('./pages/Press'));
const Blog = lazy(() => import('./pages/Blog'));
const HelpCenter = lazy(() => import('./pages/HelpCenter'));
const Contact = lazy(() => import('./pages/Contact'));
const TrackOrder = lazy(() => import('./pages/TrackOrder'));

// Components
import { Header, Footer } from './shared/components/layouts';
import ProtectedRoute from './shared/components/ProtectedRoute';
import CookieConsent from './shared/components/CookieConsent';
import AnnouncementBar from './shared/components/AnnouncementBar';
import LoadingFallback from './shared/components/LoadingFallback';

function AppContent() {
  const dispatch = useAppDispatch();
  const location = useLocation();
  const { isAuthenticated, initialized } = useAppSelector((state) => state.auth);
  const { user } = useAppSelector((state) => state.auth);
  const { handleError, clearError } = useErrorHandler();

  // Track route changes
  useEffect(() => {
    telemetry.track('route.change', { path: location.pathname });
  }, [location.pathname]);

  // Fetch user profile when authenticated
  const {
    data: profileData,
    error: profileError,
    isLoading: profileLoading,
  } = useGetProfileQuery(undefined, {
    skip: !isAuthenticated || !!user || !initialized,
  });

  // Use cart sync hook to handle local-to-backend cart sync
  const { isLoading: cartLoading } = useCartSync({
    enabled: isAuthenticated,
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
      logger.error('App', 'Failed to load user profile', profileError);
      handleError(profileError);

      // If 401 Unauthorized, user's token is invalid - logout will be handled by API middleware
      // For other errors, we continue with null user and let app gracefully degrade
    }
  }, [profileError, handleError]);

  // Show loading state while fetching profile
  const isAppLoading = profileLoading || cartLoading;

  // Loading fallback component for lazy-loaded routes
  const lazyLoadingFallback = <LoadingFallback />;

  return (
    <div>
      <AnnouncementBar />
      <Header />
      <main>
        {isAppLoading && (
          <LoadingFallback />
        )}
        {!isAppLoading && (
          <Suspense fallback={lazyLoadingFallback}>
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
              {/* Legal Pages */}
              <Route path="/privacy" element={<PrivacyPolicy />} />
              <Route path="/terms" element={<TermsOfService />} />
              <Route path="/returns" element={<ReturnsPolicy />} />
              <Route path="/cookies" element={<CookiePolicy />} />
              <Route path="/security" element={<Security />} />
              {/* Company Pages */}
              <Route path="/about" element={<AboutUs />} />
              <Route path="/careers" element={<Careers />} />
              <Route path="/press" element={<Press />} />
              <Route path="/blog" element={<Blog />} />
              {/* Support Pages */}
              <Route path="/help" element={<HelpCenter />} />
              <Route path="/contact" element={<Contact />} />
              <Route path="/track-order" element={<TrackOrder />} />
            </Routes>
          </Suspense>
        )}
      </main>
      <Footer />
      <CookieConsent />
    </div>
  );
}

function App() {
  return (
    <Router>
      <ErrorBoundary>
        <Toaster 
          position="top-right"
          toastOptions={{
            style: {
              marginTop: '80px', // Push toasts below the sticky header
            },
          }}
        />
        <ToastContainer />
        <AppContent />
      </ErrorBoundary>
    </Router>
  );
}

export default App;
