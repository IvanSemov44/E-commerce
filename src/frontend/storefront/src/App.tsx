import { BrowserRouter as Router, Routes, Route } from 'react-router-dom';
import { useEffect } from 'react';
import { useAppDispatch, useAppSelector } from './store/hooks';
import { setUser } from './store/slices/authSlice';
import { useGetProfileQuery } from './store/api/profileApi';
import { useGetCartQuery, useAddToCartMutation } from './store/api/cartApi';
import { selectCartItems } from './store/slices/cartSlice';

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

// Components
import Header from './components/Header';
import Footer from './components/Footer';
import ProtectedRoute from './components/ProtectedRoute';

function AppContent() {
  const dispatch = useAppDispatch();
  const { isAuthenticated, user, token } = useAppSelector((state) => state.auth);
  const localCartItems = useAppSelector(selectCartItems);
  const { data: profileData } = useGetProfileQuery(undefined, {
    skip: !isAuthenticated || !!user || !token,
  });

  // Fetch backend cart for authenticated users
  const { data: backendCart, isLoading: cartLoading, refetch: refetchCart } = useGetCartQuery(undefined, {
    skip: !isAuthenticated || !token,
  });

  const [addToCart] = useAddToCartMutation();

  // When profile data is fetched, update auth state
  useEffect(() => {
    if (profileData && !user) {
      dispatch(setUser({ ...profileData, role: profileData.role || 'customer' }));
    }
  }, [profileData, user, dispatch]);

  // Sync cart when user logs in: merge local cart with backend cart
  useEffect(() => {
    if (!isAuthenticated || !token || !backendCart || cartLoading) return;

    const syncCart = async () => {
      try {
        // Get items from local cart that aren't in backend cart
        const backendProductIds = new Set(backendCart.items.map((item) => item.productId));
        const itemsToSync = localCartItems.filter((item) => !backendProductIds.has(item.id));

        // Sync each local item to backend
        for (const item of itemsToSync) {
          await addToCart({
            productId: item.id,
            quantity: item.quantity,
          }).unwrap();
        }

        // Refetch cart to get latest state
        if (itemsToSync.length > 0) {
          refetchCart();
        }
      } catch (error) {
        console.error('Failed to sync cart:', error);
      }
    };

    syncCart();
  }, [isAuthenticated, token, backendCart, cartLoading, localCartItems, addToCart, refetchCart]);

  return (
    <div>
      <Header />
      <main>
        <Routes>
          <Route path="/" element={<Home />} />
          <Route path="/products" element={<Products />} />
          <Route path="/products/:slug" element={<ProductDetail />} />
          <Route path="/cart" element={<Cart />} />
          <Route path="/checkout" element={<Checkout />} />
          <Route path="/wishlist" element={<ProtectedRoute><Wishlist /></ProtectedRoute>} />
          <Route path="/orders" element={<ProtectedRoute><OrderHistory /></ProtectedRoute>} />
          <Route path="/orders/:orderId" element={<ProtectedRoute><OrderDetail /></ProtectedRoute>} />
          <Route path="/profile" element={<ProtectedRoute><Profile /></ProtectedRoute>} />
          <Route path="/login" element={<Login />} />
          <Route path="/register" element={<Register />} />
        </Routes>
      </main>
      <Footer />
    </div>
  );
}

function App() {
  return (
    <Router>
      <AppContent />
    </Router>
  );
}

export default App;
