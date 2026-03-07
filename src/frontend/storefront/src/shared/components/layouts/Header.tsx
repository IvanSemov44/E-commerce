/* eslint-disable max-lines -- Three co-located private sub-components (UserMenu, MobileMenu, Header) */
import { Link, useNavigate } from 'react-router-dom';
import { useState, useRef, useEffect, useMemo } from 'react';
import { useAppSelector, useAppDispatch } from '@/shared/lib/store';
import { logout, selectIsAuthenticated, selectCurrentUser } from '@/features/auth/slices/authSlice';
import type { AuthUser } from '@/features/auth/slices/authSlice';
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

// ─── UserMenu ────────────────────────────────────────────────────────────────

interface UserMenuProps {
  user: AuthUser | null;
  isOpen: boolean;
  onToggle: () => void;
  onClose: () => void;
  onLogout: () => void;
}

function UserMenu({ user, isOpen, onToggle, onClose, onLogout }: UserMenuProps) {
  const menuRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!isOpen) return;
    const handleClickOutside = (event: MouseEvent) => {
      if (menuRef.current && !menuRef.current.contains(event.target as Node)) {
        onClose();
      }
    };
    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, [isOpen, onClose]);

  return (
    <div className={styles.userMenuWrapper} ref={menuRef}>
      <button
        onClick={onToggle}
        className={styles.userButton}
        aria-expanded={isOpen}
        aria-label="User menu"
      >
        <div className={styles.userAvatar}>
          {user?.firstName?.charAt(0).toUpperCase() ?? 'U'}
        </div>
        <span className={styles.userName}>{user?.firstName}</span>
        <ChevronDownIcon className={styles.dropdownIcon} />
      </button>

      {isOpen && (
        <div className={styles.dropdownMenu}>
          <div className={styles.dropdownHeader}>
            <p className={styles.dropdownHeaderLabel}>Account</p>
            <p className={styles.dropdownHeaderName}>{user?.firstName}</p>
            <p className={styles.dropdownHeaderEmail}>{user?.email}</p>
          </div>
          <div className={styles.dropdownContent}>
            <Link to="/profile" onClick={onClose} className={styles.dropdownItem}>
              <UserIcon className={styles.dropdownIcon} />
              My Profile
            </Link>
            <button onClick={onLogout} className={styles.dropdownItem}>
              <LogoutIcon />
              Logout
            </button>
          </div>
        </div>
      )}
    </div>
  );
}

// ─── MobileMenu ──────────────────────────────────────────────────────────────

interface MobileMenuProps {
  isAuthenticated: boolean;
  user: AuthUser | null;
  cartItemCount: number;
  wishlistItemCount: number;
  onClose: () => void;
  onLogout: () => void;
}

function MobileMenu({
  isAuthenticated,
  user,
  cartItemCount,
  wishlistItemCount,
  onClose,
  onLogout,
}: MobileMenuProps) {
  const { t } = useTranslation();

  return (
    <div className={styles.mobileMenu}>
      <div className={styles.mobileMenuContent}>
        <Link to="/products" onClick={onClose} className={styles.mobileNavLink}>
          <div className={styles.mobileNavContent}>
            <PackageIcon />
            Products
          </div>
        </Link>

        {isAuthenticated && (
          <Link to="/orders" onClick={onClose} className={styles.mobileNavLink}>
            <div className={styles.mobileNavContent}>
              <DocumentIcon />
              Orders
            </div>
          </Link>
        )}

        {isAuthenticated && (
          <Link to="/wishlist" onClick={onClose} className={styles.mobileNavLink}>
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

        <Link to="/cart" onClick={onClose} className={styles.mobileNavLink}>
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

        <div className={styles.mobileDivider} />

        <div className={styles.mobileThemeToggle}>
          <span className={styles.mobileThemeLabel}>{t('nav.appearance')}</span>
          <ThemeToggle size="md" />
        </div>

        <div className={styles.mobileDivider} />

        {isAuthenticated ? (
          <>
            <div className={styles.mobileUserInfo}>
              <p className={styles.mobileUserLabel}>Logged in as</p>
              <p className={styles.mobileUserName}>{user?.firstName}</p>
              <p className={styles.mobileUserEmail}>{user?.email}</p>
            </div>
            <Link to="/profile" onClick={onClose} className={styles.mobileNavLink}>
              <div className={styles.mobileNavContent}>
                <UserIcon />
                My Profile
              </div>
            </Link>
            <button onClick={onLogout} className={styles.mobileLogoutButton}>
              <LogoutIcon />
              Logout
            </button>
          </>
        ) : (
          <div className={styles.mobileAuthButtons}>
            <Link to="/login" onClick={onClose} className={styles.mobileSignInLink}>
              Sign In
            </Link>
            <Link to="/register" onClick={onClose} className={styles.mobileSignUpLink}>
              Sign Up
            </Link>
          </div>
        )}
      </div>
    </div>
  );
}

// ─── Header (orchestrator) ───────────────────────────────────────────────────

export default function Header() {
  const { t } = useTranslation();
  const isAuthenticated = useAppSelector(selectIsAuthenticated);
  const user = useAppSelector(selectCurrentUser);
  const localCartItemCount = useAppSelector(selectCartItemCount);
  const dispatch = useAppDispatch();
  const navigate = useNavigate();
  const [mobileMenuOpen, setMobileMenuOpen] = useState(false);
  const [userMenuOpen, setUserMenuOpen] = useState(false);

  const { data: backendCart } = useGetCartQuery(undefined, {
    skip: !isAuthenticated,
    refetchOnMountOrArgChange: true,
  });

  const { data: wishlistData } = useGetWishlistQuery(undefined, {
    skip: !isAuthenticated,
    refetchOnMountOrArgChange: true,
  });

  const cartItemCount = useMemo(() => {
    if (isAuthenticated && backendCart?.items) {
      return backendCart.items.reduce((sum, item) => sum + item.quantity, 0);
    }
    return localCartItemCount;
  }, [isAuthenticated, backendCart, localCartItemCount]);

  const wishlistItemCount = useMemo(
    () => wishlistData?.items?.length ?? 0,
    [wishlistData]
  );

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

          {/* Search Bar */}
          <div className={styles.searchContainer}>
            <SearchBar size="sm" />
          </div>

          {/* Desktop Right Items */}
          <div className={styles.desktopRight}>
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

            <Link to="/cart" className={styles.cartLink} aria-label={t('nav.cart')}>
              <ShoppingCartIcon className={styles.cartIcon} />
              {cartItemCount > 0 && (
                <span className={styles.cartBadge}>
                  {cartItemCount > 99 ? '99+' : cartItemCount}
                </span>
              )}
            </Link>

            <ThemeToggle size="sm" />
            <LanguageSwitcher size="sm" />

            {isAuthenticated ? (
              <UserMenu
                user={user}
                isOpen={userMenuOpen}
                onToggle={() => setUserMenuOpen((open) => !open)}
                onClose={() => setUserMenuOpen(false)}
                onLogout={handleLogout}
              />
            ) : (
              <div className={styles.authButtons}>
                <Link to="/login">
                  <Button variant="ghost" size="sm">
                    {t('nav.signIn')}
                  </Button>
                </Link>
                <Link to="/register">
                  <Button size="sm">Sign Up</Button>
                </Link>
              </div>
            )}
          </div>

          {/* Mobile Menu Button */}
          <button
            onClick={() => setMobileMenuOpen((open) => !open)}
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

      {mobileMenuOpen && (
        <MobileMenu
          isAuthenticated={isAuthenticated}
          user={user}
          cartItemCount={cartItemCount}
          wishlistItemCount={wishlistItemCount}
          onClose={() => setMobileMenuOpen(false)}
          onLogout={handleMobileLogout}
        />
      )}
    </header>
  );
}
