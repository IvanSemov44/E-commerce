/**
 * CheckoutAuthBanner Component
 * Displays authentication status at the top of checkout:
 * - For authenticated users: Shows welcome message with sign out option
 * - For guests: Shows guest checkout info with sign in prompt
 */

import { Link } from 'react-router-dom';
import { useAppSelector, useAppDispatch } from '@/shared/lib/store';
import { logout } from '@/features/auth/slices/authSlice';
import Button from '@/shared/components/ui/Button';
import styles from './CheckoutAuthBanner.module.css';

interface CheckoutAuthBannerProps {
  className?: string;
}

export default function CheckoutAuthBanner({ className }: CheckoutAuthBannerProps) {
  const dispatch = useAppDispatch();
  const isAuthenticated = useAppSelector((state) => state.auth.isAuthenticated);
  const user = useAppSelector((state) => state.auth.user);

  const handleSignOut = () => {
    dispatch(logout());
  };

  if (isAuthenticated && user) {
    return (
      <div className={`${styles.banner} ${styles.authenticated} ${className || ''}`}>
        <div className={styles.content}>
          <span className={styles.icon}>👋</span>
          <div className={styles.text}>
            <strong>Welcome back, {user.firstName}!</strong>
            <span className={styles.subtext}>
              You're signed in as {user.email}
            </span>
          </div>
        </div>
        <Button
          variant="ghost"
          size="sm"
          onClick={handleSignOut}
          className={styles.signOutBtn}
        >
          Sign out
        </Button>
      </div>
    );
  }

  return (
    <div className={`${styles.banner} ${styles.guest} ${className || ''}`}>
      <div className={styles.content}>
        <span className={styles.icon}>🛒</span>
        <div className={styles.text}>
          <strong>Guest Checkout</strong>
          <span className={styles.subtext}>
            You're checking out as a guest. Sign in for faster checkout next time.
          </span>
        </div>
      </div>
      <Link to="/login?redirect=/checkout">
        <Button variant="outline" size="sm">
          Sign In
        </Button>
      </Link>
    </div>
  );
}
