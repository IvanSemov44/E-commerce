import { Link } from 'react-router';
import { useTranslation } from 'react-i18next';
import { useAppSelector } from '@/shared/lib/store';
import { selectCurrentUser, selectIsAuthenticated } from '@/features/auth/slices/authSlice';
import { MOBILE_NAV_ITEMS, ROUTE_PATHS } from '@/shared/constants/navigation';
import {
  HeartIcon,
  ShoppingCartIcon,
  UserIcon,
  LogoutIcon,
  PackageIcon,
  DocumentIcon,
} from '@/shared/components/icons';
import { ThemeToggle } from '@/app/ThemeToggle';
import { useHeaderData } from '../useHeaderData';
import styles from './HeaderMobileMenu.module.css';

interface HeaderMobileMenuProps {
  onClose: () => void;
  onLogout: () => void;
}

export function HeaderMobileMenu({ onClose, onLogout }: HeaderMobileMenuProps) {
  const isAuthenticated = useAppSelector(selectIsAuthenticated);
  const user = useAppSelector(selectCurrentUser);
  const { cartItemCount, wishlistItemCount } = useHeaderData();
  const { t } = useTranslation();

  const getMobileIcon = (icon: (typeof MOBILE_NAV_ITEMS)[number]['icon']) => {
    switch (icon) {
      case 'products':
        return <PackageIcon />;
      case 'orders':
        return <DocumentIcon />;
      case 'wishlist':
        return <HeartIcon />;
      case 'cart':
        return <ShoppingCartIcon />;
      default:
        return null;
    }
  };

  const getBadgeValue = (badge: (typeof MOBILE_NAV_ITEMS)[number]['badge']) => {
    if (badge === 'wishlist') {
      return wishlistItemCount;
    }

    if (badge === 'cart') {
      return cartItemCount;
    }

    return 0;
  };

  return (
    <div className={styles.mobileMenu}>
      <div className={styles.mobileMenuContent}>
        {MOBILE_NAV_ITEMS.filter((item) => !item.requiresAuth || isAuthenticated).map((item) => {
          const badgeValue = item.badge ? getBadgeValue(item.badge) : 0;

          return (
            <Link key={item.path} to={item.path} onClick={onClose} className={styles.mobileNavLink}>
              <div className={styles.mobileNavContent}>
                {getMobileIcon(item.icon)}
                {t(item.labelKey)}
                {badgeValue > 0 && (
                  <span className={styles.badgeWrapper}>
                    <span className={styles.cartBadge}>{badgeValue > 99 ? '99+' : badgeValue}</span>
                  </span>
                )}
              </div>
            </Link>
          );
        })}

        <div className={styles.mobileDivider} />

        <div className={styles.mobileThemeToggle}>
          <span className={styles.mobileThemeLabel}>{t('nav.appearance')}</span>
          <ThemeToggle size="md" />
        </div>

        <div className={styles.mobileDivider} />

        {isAuthenticated ? (
          <>
            <div className={styles.mobileUserInfo}>
              <p className={styles.mobileUserLabel}>{t('nav.loggedInAs')}</p>
              <p className={styles.mobileUserName}>{user?.firstName}</p>
              <p className={styles.mobileUserEmail}>{user?.email}</p>
            </div>
            <Link to={ROUTE_PATHS.profile} onClick={onClose} className={styles.mobileNavLink}>
              <div className={styles.mobileNavContent}>
                <UserIcon />
                {t('nav.myProfile')}
              </div>
            </Link>
            <button onClick={onLogout} className={styles.mobileLogoutButton}>
              <LogoutIcon />
              {t('nav.signOut')}
            </button>
          </>
        ) : (
          <div className={styles.mobileAuthButtons}>
            <Link to={ROUTE_PATHS.login} onClick={onClose} className={styles.mobileSignInLink}>
              {t('nav.signIn')}
            </Link>
            <Link to={ROUTE_PATHS.register} onClick={onClose} className={styles.mobileSignUpLink}>
              {t('nav.signUp')}
            </Link>
          </div>
        )}
      </div>
    </div>
  );
}
