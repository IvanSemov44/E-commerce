import { Link, useNavigate } from 'react-router-dom';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useAppDispatch, useAppSelector } from '@/shared/lib/store';
import { logout, selectCurrentUser, selectIsAuthenticated } from '@/features/auth/slices/authSlice';
import { HEADER_NAV_ITEMS, ROUTE_PATHS } from '@/shared/constants/navigation';
import { HeartIcon, ShoppingCartIcon, MenuIcon, CloseIcon } from '@/shared/components/icons';
import Button from '@/shared/components/ui/Button';
import { ThemeToggle } from '@/app/ThemeToggle';
import { LanguageSwitcher } from '@/app/LanguageSwitcher';
import { SearchBar } from '@/app/SearchBar';
import HeaderUserMenu from '../HeaderUserMenu';
import HeaderMobileMenu from '../HeaderMobileMenu';
import { useHeaderData } from '../useHeaderData';
import styles from './Header.module.css';

export default function Header() {
  const { t } = useTranslation();
  const dispatch = useAppDispatch();
  const navigate = useNavigate();
  const isAuthenticated = useAppSelector(selectIsAuthenticated);
  const user = useAppSelector(selectCurrentUser);
  const { cartItemCount, wishlistItemCount } = useHeaderData(isAuthenticated);

  const [mobileMenuOpen, setMobileMenuOpen] = useState(false);
  const [userMenuOpen, setUserMenuOpen] = useState(false);

  const handleLogout = (onClose: () => void) => {
    dispatch(logout());
    onClose();
    navigate(ROUTE_PATHS.home);
  };

  return (
    <header className={styles.header}>
      <div className={styles.container}>
        <nav className={styles.nav}>
          <Link to={ROUTE_PATHS.home} className={styles.logo}>
            <div className={styles.logoBadge}>
              <ShoppingCartIcon />
            </div>
            <span>E-Shop</span>
          </Link>

          <div className={styles.desktopNav}>
            {HEADER_NAV_ITEMS.filter((item) => !item.requiresAuth || isAuthenticated).map(
              (item) => (
                <Link key={item.path} to={item.path} className={styles.navLink}>
                  {t(item.labelKey)}
                </Link>
              )
            )}
          </div>

          <div className={styles.searchContainer}>
            <SearchBar size="sm" />
          </div>

          <div className={styles.desktopRight}>
            {isAuthenticated && (
              <Link
                to={ROUTE_PATHS.wishlist}
                className={styles.cartLink}
                aria-label={t('nav.wishlist')}
              >
                <HeartIcon className={styles.cartIcon} />
                {wishlistItemCount > 0 && (
                  <span className={styles.cartBadge}>
                    {wishlistItemCount > 99 ? '99+' : wishlistItemCount}
                  </span>
                )}
              </Link>
            )}

            <Link to={ROUTE_PATHS.cart} className={styles.cartLink} aria-label={t('nav.cart')}>
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
              <HeaderUserMenu
                user={user}
                isOpen={userMenuOpen}
                onToggle={() => setUserMenuOpen((open) => !open)}
                onClose={() => setUserMenuOpen(false)}
                onLogout={() => handleLogout(() => setUserMenuOpen(false))}
              />
            ) : (
              <div className={styles.authButtons}>
                <Link to={ROUTE_PATHS.login}>
                  <Button variant="ghost" size="sm">
                    {t('nav.signIn')}
                  </Button>
                </Link>
                <Link to={ROUTE_PATHS.register}>
                  <Button size="sm">Sign Up</Button>
                </Link>
              </div>
            )}
          </div>

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
        <HeaderMobileMenu
          isAuthenticated={isAuthenticated}
          user={user}
          cartItemCount={cartItemCount}
          wishlistItemCount={wishlistItemCount}
          onClose={() => setMobileMenuOpen(false)}
          onLogout={() => handleLogout(() => setMobileMenuOpen(false))}
        />
      )}
    </header>
  );
}
