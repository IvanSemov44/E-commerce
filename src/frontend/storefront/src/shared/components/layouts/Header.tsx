import { Link, useNavigate } from 'react-router-dom';
import { useState, useRef, useEffect, useMemo } from 'react';
import { useAppSelector, useAppDispatch } from '@/shared/lib/store';
import { logout, selectIsAuthenticated, selectCurrentUser } from '@/features/auth/slices/authSlice';
import { selectCartItemCount } from '@/features/cart/slices/cartSlice';
import { useGetCartQuery } from '@/features/cart/api/cartApi';
import { useGetWishlistQuery } from '@/features/wishlist/api/wishlistApi';
import { useTranslation } from 'react-i18next';
import {
  HeartIcon,
  ShoppingCartIcon,
  ChevronDownIcon,
  UserIcon,
  LogoutIcon,
  MenuIcon,
  CloseIcon,
  PackageIcon,
  DocumentIcon,
} from '../icons';
import Button from '../ui/Button';
import { ThemeToggle } from '../ThemeToggle';
import { LanguageSwitcher } from '../LanguageSwitcher';
import { SearchBar } from '../SearchBar';
import styles from './Header.module.css';

export default function Header() {
  const { t } = useTranslation();
  const isAuthenticated = useAppSelector(selectIsAuthenticated);
  const user = useAppSelector(selectCurrentUser);
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
              <ShoppingCartIcon />
            </div>
            <span>E-Shop</span>
          </Link>

          {/* Desktop Navigation */}
          <div className={styles.desktopNav}>
            <Link to="/products" className={styles.navLink}>
              {t('nav.products')}
            </Link>
            {isAuthenticated && (
              <Link to="/orders" className={styles.navLink}>
                {t('nav.orders')}
              </Link>
            )}
          </div>

          {/* Search Bar - Centered */}
          <div className={styles.searchContainer}>
            <SearchBar size="sm" />
          </div>

          {/* Desktop Right Items */}
          <div className={styles.desktopRight}>
            {/* Wishlist */}
            {isAuthenticated && (
              <Link to="/wishlist" className={styles.cartLink} aria-label={t('nav.wishlist')}>
                <HeartIcon className={styles.cartIcon} />
                {wishlistItemCount > 0 && (
                  <span className={styles.cartBadge}>
                    {wishlistItemCount > 99 ? '99+' : wishlistItemCount}
                  </span>
                )}
              </Link>
            )}

            {/* Cart */}
            <Link to="/cart" className={styles.cartLink} aria-label={t('nav.cart')}>
              <ShoppingCartIcon className={styles.cartIcon} />
              {cartItemCount > 0 && (
                <span className={styles.cartBadge}>
                  {cartItemCount > 99 ? '99+' : cartItemCount}
                </span>
              )}
            </Link>

            {/* Theme Toggle */}
            <ThemeToggle size="sm" />

            {/* Language Switcher */}
            <LanguageSwitcher size="sm" />

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
                  <ChevronDownIcon className={styles.dropdownIcon} />
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
                        <UserIcon className={styles.dropdownIcon} />
                        My Profile
                      </Link>
                      <button
                        onClick={handleLogout}
                        className={styles.dropdownItem}
                      >
                        <LogoutIcon />
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
                      {t('nav.signIn')}
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
            {mobileMenuOpen ? (
              <CloseIcon className={styles.mobileMenuIcon} />
            ) : (
              <MenuIcon className={styles.mobileMenuIcon} />
            )}
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
                <PackageIcon />
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
                  <DocumentIcon />
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
                  <HeartIcon />
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
                <ShoppingCartIcon />
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

            {/* Theme Toggle for Mobile */}
            <div className={styles.mobileThemeToggle}>
              <span className={styles.mobileThemeLabel}>{t('nav.appearance')}</span>
              <ThemeToggle size="md" />
            </div>

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
                    <UserIcon />
                    My Profile
                  </div>
                </Link>
                <button
                  onClick={handleMobileLogout}
                  className={styles.mobileLogoutButton}
                >
                  <LogoutIcon />
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
