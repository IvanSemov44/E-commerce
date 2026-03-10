import { lazy } from 'react';
import { Routes, Route } from 'react-router-dom';
import { ROUTE_PATHS } from '@/shared/constants/navigation';
import ProtectedRoute from '@/app/ProtectedRoute';

// Eagerly load critical pages (home, login, register)
import Home from '@/pages/HomePage/HomePage';
import Login from '@/features/auth/pages/LoginPage';
import Register from '@/features/auth/pages/RegisterPage';

// Lazy load non-critical pages for code splitting
const Products = lazy(() => import('@/features/products/pages/ProductsPage/ProductsPage'));
const ProductDetail = lazy(
  () => import('@/features/products/pages/ProductDetailPage/ProductDetailPage')
);
const Cart = lazy(() => import('@/features/cart/pages/CartPage/CartPage'));
const Checkout = lazy(() => import('@/pages/CheckoutPage/CheckoutPage'));
const OrderHistory = lazy(
  () => import('@/features/orders/pages/OrderHistoryPage/OrderHistoryPage')
);
const OrderDetail = lazy(() => import('@/features/orders/pages/OrderDetailPage/OrderDetailPage'));
const Profile = lazy(() => import('@/features/profile/pages/ProfilePage/ProfilePage'));
const Wishlist = lazy(() => import('@/features/wishlist/pages/WishlistPage/WishlistPage'));
const ForgotPassword = lazy(() => import('@/features/auth/pages/ForgotPasswordPage'));
const ResetPassword = lazy(() => import('@/features/auth/pages/ResetPasswordPage'));

// Content pages
const PrivacyPolicy = lazy(() => import('@/pages/PrivacyPolicy'));
const TermsOfService = lazy(() => import('@/pages/TermsOfService'));
const ReturnsPolicy = lazy(() => import('@/pages/ReturnsPolicy'));
const CookiePolicy = lazy(() => import('@/pages/CookiePolicy'));
const Security = lazy(() => import('@/pages/Security'));
const AboutUs = lazy(() => import('@/pages/AboutUs'));
const Careers = lazy(() => import('@/pages/Careers'));
const Press = lazy(() => import('@/pages/Press'));
const Blog = lazy(() => import('@/pages/Blog'));
const HelpCenter = lazy(() => import('@/pages/HelpCenter'));
const Contact = lazy(() => import('@/pages/Contact'));
const TrackOrder = lazy(() => import('@/pages/TrackOrder'));

export default function AppRoutes() {
  return (
    <Routes>
      <Route path={ROUTE_PATHS.home} element={<Home />} />
      <Route path={ROUTE_PATHS.products} element={<Products />} />
      <Route path={ROUTE_PATHS.productDetail} element={<ProductDetail />} />
      <Route path={ROUTE_PATHS.cart} element={<Cart />} />
      <Route path={ROUTE_PATHS.checkout} element={<Checkout />} />
      <Route
        path={ROUTE_PATHS.wishlist}
        element={
          <ProtectedRoute>
            <Wishlist />
          </ProtectedRoute>
        }
      />
      <Route
        path={ROUTE_PATHS.orders}
        element={
          <ProtectedRoute>
            <OrderHistory />
          </ProtectedRoute>
        }
      />
      <Route
        path={ROUTE_PATHS.orderDetail}
        element={
          <ProtectedRoute>
            <OrderDetail />
          </ProtectedRoute>
        }
      />
      <Route
        path={ROUTE_PATHS.profile}
        element={
          <ProtectedRoute>
            <Profile />
          </ProtectedRoute>
        }
      />
      <Route path={ROUTE_PATHS.login} element={<Login />} />
      <Route path={ROUTE_PATHS.register} element={<Register />} />
      <Route path={ROUTE_PATHS.forgotPassword} element={<ForgotPassword />} />
      <Route path={ROUTE_PATHS.resetPassword} element={<ResetPassword />} />
      <Route path={ROUTE_PATHS.privacy} element={<PrivacyPolicy />} />
      <Route path={ROUTE_PATHS.terms} element={<TermsOfService />} />
      <Route path={ROUTE_PATHS.returns} element={<ReturnsPolicy />} />
      <Route path={ROUTE_PATHS.cookies} element={<CookiePolicy />} />
      <Route path={ROUTE_PATHS.security} element={<Security />} />
      <Route path={ROUTE_PATHS.about} element={<AboutUs />} />
      <Route path={ROUTE_PATHS.careers} element={<Careers />} />
      <Route path={ROUTE_PATHS.press} element={<Press />} />
      <Route path={ROUTE_PATHS.blog} element={<Blog />} />
      <Route path={ROUTE_PATHS.help} element={<HelpCenter />} />
      <Route path={ROUTE_PATHS.contact} element={<Contact />} />
      <Route path={ROUTE_PATHS.trackOrder} element={<TrackOrder />} />
    </Routes>
  );
}
