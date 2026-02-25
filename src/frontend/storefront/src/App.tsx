import { BrowserRouter as Router, Routes, Route } from 'react-router-dom';
import { useEffect, lazy, Suspense } from 'react';
import { Toaster } from 'react-hot-toast';
import { useAppDispatch, useAppSelector } from './store/hooks';
import { setUser } from './store/slices/authSlice';
import { useGetProfileQuery } from './store/api/profileApi';
import { useCartSync, useErrorHandler } from './hooks';
import ToastContainer from './components/Toast/ToastContainer';
import ErrorBoundary from './components/ErrorBoundary';

// Eagerly load critical pages (home, login, register)
import Home from './pages/Home';
import Login from './pages/Login';
import Register from './pages/Register';

// Lazy load non-critical pages for code splitting
const Products = lazy(() => import('./pages/Products'));
const ProductDetail = lazy(() => import('./pages/ProductDetail'));
const Cart = lazy(() => import('./pages/Cart'));
const Checkout = lazy(() => import('./pages/Checkout'));
const OrderHistory = lazy(() => import('./pages/OrderHistory'));
const OrderDetail = lazy(() => import('./pages/OrderDetail'));
const Profile = lazy(() => import('./pages/Profile'));
const Wishlist = lazy(() => import('./pages/Wishlist'));
const ForgotPassword = lazy(() => import('./pages/ForgotPassword'));
const ResetPassword = lazy(() => import('./pages/ResetPassword'));

// Legal pages
const PrivacyPolicy = lazy(() => import('./pages/PrivacyPolicy'));
const TermsOfService = lazy(() => import('./pages/TermsOfService'));
const ReturnsPolicy = lazy(() => import('./pages/ReturnsPolicy'));
const CookiePolicy = lazy(() => import('./pages/CookiePolicy'));
const Security = lazy(() => import('./pages/Security'));

// Company pages
const AboutUs = lazy(() => import('./pages/AboutUs'));
const Careers = lazy(() => import('./pages/Careers'));
const Press = lazy(() => import('./pages/Press'));
const Blog = lazy(() => import('./pages/Blog'));

// Support pages
const HelpCenter = lazy(() => import('./pages/HelpCenter'));
const Contact = lazy(() => import('./pages/Contact'));
const TrackOrder = lazy(() => import('./pages/TrackOrder'));

// Components
import Header from './components/Header';
import Footer from './components/Footer';
import ProtectedRoute from './components/ProtectedRoute';
import CookieConsent from './components/CookieConsent';
import AnnouncementBar from './components/AnnouncementBar';

function AppContent() {
  const dispatch = useAppDispatch();
  const { isAuthenticated, initialized } = useAppSelector((state) => state.auth);
  const { user } = useAppSelector((state) => state.auth);
  const { handleError, clearError } = useErrorHandler();

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
      console.error('Failed to load user profile:', profileError);
      handleError(profileError);

      // If 401 Unauthorized, user's token is invalid - logout will be handled by API middleware
      // For other errors, we continue with null user and let app gracefully degrade
    }
  }, [profileError, handleError]);

  // Show loading state while fetching profile
  const isAppLoading = profileLoading || cartLoading;

  // Loading fallback component for lazy-loaded routes
  const LazyLoadingFallback = (
    <div style={{ textAlign: 'center', padding: '2rem' }}>
      <p>Loading page...</p>
    </div>
  );

  return (
    <div>
      <AnnouncementBar />
      <Header />
      <main>
        {isAppLoading && (
          <div style={{ textAlign: 'center', padding: '2rem' }}>
            Loading...
          </div>
        )}
        {!isAppLoading && (
          <Suspense fallback={LazyLoadingFallback}>
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
