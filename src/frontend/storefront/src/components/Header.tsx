import { Link, useNavigate } from 'react-router-dom';
import { useState, useRef, useEffect, useMemo } from 'react';
import { useAppSelector, useAppDispatch } from '../store/hooks';
import { logout } from '../store/slices/authSlice';
import { selectCartItemCount } from '../store/slices/cartSlice';
import { useGetCartQuery } from '../store/api/cartApi';
import { useGetWishlistQuery } from '../store/api/wishlistApi';
import { HeartIcon, ShoppingCartIcon } from './icons';
import Button from './ui/Button';
import styles from './Header.module.css';

export default function Header() {
  const { isAuthenticated, user } = useAppSelector((state) => state.auth);
  const localCartItemCount = useAppSelector(selectCartItemCount);
  const dispatch = useAppDispatch();
  const navigate = useNavigate();
  const [mobileMenuOpen, setMobileMenuOpen] = useState(false);
  const [userMenuOpen, setUserMenuOpen] = useState(false);
  const userMenuRef = useRef<HTMLDivElement>(null);

  // Backend cart query (only for authenticated users)
  const { data: backendCart } = useGetCartQuery(undefined, {
    skip: !isAuthenticated,
    refetchOnMountOrArgChange: true,
  });

  // Wishlist query (only for authenticated users)
  const { data: wishlistData } = useGetWishlistQuery(undefined, {
    skip: !isAuthenticated,
    refetchOnMountOrArgChange: true,
  });

  // Calculate cart item count based on authentication status
  const cartItemCount = useMemo(() => {
    if (isAuthenticated && backendCart?.items) {
      return backendCart.items.reduce((sum, item) => sum + item.quantity, 0);
    }
    return localCartItemCount;
  }, [isAuthenticated, backendCart, localCartItemCount]);

  // Calculate wishlist item count
  const wishlistItemCount = useMemo(() => {
    return wishlistData?.items?.length || 0;
  }, [wishlistData]);

  // Close user menu when clicking outside
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (userMenuRef.current && !userMenuRef.current.contains(event.target as Node)) {
        setUserMenuOpen(false);
      }
    };

    if (userMenuOpen) {
      document.addEventListener('mousedown', handleClickOutside);
      return () => document.removeEventListener('mousedown', handleClickOutside);
    }
  }, [userMenuOpen]);

  const handleLogout = () => {
    dispatch(logout());
    setUserMenuOpen(false);
    navigate('/');
  };

  const handleMobileLogout = () => {
    dispatch(logout());
    setMobileMenuOpen(false);
    navigate('/');
  };

  return (
    <header className={styles.header}>
      <div className={styles.container}>
        <nav className={styles.nav}>
          {/* Logo */}
          <Link to="/" className={styles.logo}>
            <div className={styles.logoBadge}>
              <svg fill="currentColor" viewBox="0 0 20 20">
                <path d="M3 1a1 1 0 000 2h1.22l.305 1.222a.997.997 0 00.01.042l1.358 5.43-.893.892C3.74 11.846 4.632 14 6.414 14H15a1 1 0 000-2H6.414l1-1h7.586a1 1 0 00.894-.553l3-6A1 1 0 0017 3H6.28l-.31-1.243A1 1 0 005 1H3zM5 16a2 2 0 11-4 0 2 2 0 014 0z" />
              </svg>
            </div>
            <span>E-Shop</span>
          </Link>

          {/* Desktop Navigation */}
          <div className={styles.desktopNav}>
            <Link to="/products" className={styles.navLink}>
              Products
            </Link>
            {isAuthenticated && (
              <Link to="/orders" className={styles.navLink}>
                Orders
              </Link>
            )}
          </div>

          {/* Desktop Right Items */}
          <div className={styles.desktopRight}>
            {/* Wishlist */}
            {isAuthenticated && (
              <Link to="/wishlist" className={styles.cartLink} aria-label="Wishlist">
                <HeartIcon className={styles.cartIcon} />
                {wishlistItemCount > 0 && (
                  <span className={styles.cartBadge}>
                    {wishlistItemCount > 99 ? '99+' : wishlistItemCount}
                  </span>
                )}
              </Link>
            )}

            {/* Cart */}
            <Link to="/cart" className={styles.cartLink} aria-label="Shopping cart">
              <ShoppingCartIcon className={styles.cartIcon} />
              {cartItemCount > 0 && (
                <span className={styles.cartBadge}>
                  {cartItemCount > 99 ? '99+' : cartItemCount}
                </span>
              )}
            </Link>

            {/* Auth */}
            {isAuthenticated ? (
              <div className={styles.userMenuWrapper} ref={userMenuRef}>
                <button
                  onClick={() => setUserMenuOpen(!userMenuOpen)}
                  className={styles.userButton}
                  aria-expanded={userMenuOpen}
                  aria-label="User menu"
                >
                  <div className={styles.userAvatar}>
                    {user?.firstName?.charAt(0).toUpperCase() || 'U'}
                  </div>
                  <span className={styles.userName}>{user?.firstName}</span>
                  <svg className={styles.dropdownIcon} fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 14l-7 7m0 0l-7-7m7 7V3" />
                  </svg>
                </button>

                {/* User Dropdown Menu */}
                {userMenuOpen && (
                  <div className={styles.dropdownMenu}>
                    <div className={styles.dropdownHeader}>
                      <p className={styles.dropdownHeaderLabel}>Account</p>
                      <p className={styles.dropdownHeaderName}>{user?.firstName}</p>
                      <p className={styles.dropdownHeaderEmail}>{user?.email}</p>
                    </div>
                    <div className={styles.dropdownContent}>
                      <Link
                        to="/profile"
                        onClick={() => setUserMenuOpen(false)}
                        className={styles.dropdownItem}
                      >
                        <svg fill="none" stroke="currentColor" viewBox="0 0 24 24" className={styles.dropdownIcon}>
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z" />
                        </svg>
                        My Profile
                      </Link>
                      <button
                        onClick={handleLogout}
                        className={styles.dropdownItem}
                      >
                        <svg fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M17 16l4-4m0 0l-4-4m4 4H7m6 4v1a3 3 0 01-3 3H6a3 3 0 01-3-3V7a3 3 0 013-3h4a3 3 0 013 3v1" />
                        </svg>
                        Logout
                      </button>
                    </div>
                  </div>
                )}
              </div>
            ) : (
              <div className={styles.authButtons}>
                <Link to="/login">
                  <Button variant="ghost" size="sm">
                    Sign In
                  </Button>
                </Link>
                <Link to="/register">
                  <Button size="sm">
                    Sign Up
                  </Button>
                </Link>
              </div>
            )}
          </div>

          {/* Mobile Menu Button */}
          <button
            onClick={() => setMobileMenuOpen(!mobileMenuOpen)}
            className={styles.mobileMenuButton}
            aria-label="Toggle menu"
            aria-expanded={mobileMenuOpen}
          >
            <svg className={styles.mobileMenuIcon} fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={2}
                d={mobileMenuOpen ? "M6 18L18 6M6 6l12 12" : "M4 6h16M4 12h16M4 18h16"}
              />
            </svg>
          </button>
        </nav>
      </div>

      {/* Mobile Menu */}
      {mobileMenuOpen && (
        <div className={styles.mobileMenu}>
          <div className={styles.mobileMenuContent}>
            {/* Mobile Navigation */}
            <Link
              to="/products"
              onClick={() => setMobileMenuOpen(false)}
              className={styles.mobileNavLink}
            >
              <div className={styles.mobileNavContent}>
                <svg width="20" height="20" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M20 7l-8-4-8 4m0 0l8-4m0 0l8 4m0 0v10l-8 4m0-10L4 7v10l8 4" />
                </svg>
                Products
              </div>
            </Link>

            {/* Mobile Orders */}
            {isAuthenticated && (
              <Link
                to="/orders"
                onClick={() => setMobileMenuOpen(false)}
                className={styles.mobileNavLink}
              >
                <div className={styles.mobileNavContent}>
                  <svg width="20" height="20" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
                  </svg>
                  Orders
                </div>
              </Link>
            )}

            {/* Mobile Wishlist */}
            {isAuthenticated && (
              <Link
                to="/wishlist"
                onClick={() => setMobileMenuOpen(false)}
                className={styles.mobileNavLink}
              >
                <div className={styles.mobileNavContent}>
                  <svg width="20" height="20" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4.318 6.318a4.5 4.5 0 000 6.364L12 20.364l7.682-7.682a4.5 4.5 0 00-6.364-6.364L12 7.636l-1.318-1.318a4.5 4.5 0 00-6.364 0z" />
                  </svg>
                  Wishlist
                  {wishlistItemCount > 0 && (
                    <span className={styles.badgeWrapper}>
                      <span className={styles.cartBadge}>
                        {wishlistItemCount > 99 ? '99+' : wishlistItemCount}
                      </span>
                    </span>
                  )}
                </div>
              </Link>
            )}

            {/* Mobile Cart */}
            <Link
              to="/cart"
              onClick={() => setMobileMenuOpen(false)}
              className={styles.mobileNavLink}
            >
              <div className={styles.mobileNavContent}>
                <svg width="20" height="20" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M3 3h2l.4 2M7 13h10l4-8H5.4M7 13L5.4 5M7 13l-2.293 2.293c-.63.63-.184 1.707.707 1.707H17m0 0a2 2 0 100 4 2 2 0 000-4zm-8 2a2 2 0 11-4 0 2 2 0 014 0z" />
                </svg>
                Cart
                {cartItemCount > 0 && (
                  <span className={styles.badgeWrapper}>
                    <span className={styles.cartBadge}>
                      {cartItemCount > 99 ? '99+' : cartItemCount}
                    </span>
                  </span>
                )}
              </div>
            </Link>

            {/* Divider */}
            <div className={styles.mobileDivider}></div>

            {/* Auth Section */}
            {isAuthenticated ? (
              <>
                <div className={styles.mobileUserInfo}>
                  <p className={styles.mobileUserLabel}>Logged in as</p>
                  <p className={styles.mobileUserName}>{user?.firstName}</p>
                  <p className={styles.mobileUserEmail}>{user?.email}</p>
                </div>
                <Link
                  to="/profile"
                  onClick={() => setMobileMenuOpen(false)}
                  className={styles.mobileNavLink}
                >
                  <div className={styles.mobileNavContent}>
                    <svg width="20" height="20" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z" />
                    </svg>
                    My Profile
                  </div>
                </Link>
                <button
                  onClick={handleMobileLogout}
                  className={styles.mobileLogoutButton}
                >
                  <svg width="20" height="20" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M17 16l4-4m0 0l-4-4m4 4H7m6 4v1a3 3 0 01-3 3H6a3 3 0 01-3-3V7a3 3 0 013-3h4a3 3 0 013 3v1" />
                  </svg>
                  Logout
                </button>
              </>
            ) : (
              <div className={styles.mobileAuthButtons}>
                <Link
                  to="/login"
                  onClick={() => setMobileMenuOpen(false)}
                  className={styles.mobileSignInLink}
                >
                  Sign In
                </Link>
                <Link
                  to="/register"
                  onClick={() => setMobileMenuOpen(false)}
                  className={styles.mobileSignUpLink}
                >
                  Sign Up
                </Link>
              </div>
            )}
          </div>
        </div>
      )}
    </header>
  );
}
