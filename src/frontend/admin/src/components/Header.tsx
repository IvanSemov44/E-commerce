import { useState, useRef, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  BellIcon,
  ChevronDownIcon,
  SettingsIcon,
  LogoutIcon,
} from './icons';
import { useAppSelector, useAppDispatch } from '../store/hooks';
import { logout } from '../store/slices/authSlice';
import styles from './Header.module.css';

export default function Header() {
  const [userMenuOpen, setUserMenuOpen] = useState(false);
  const userMenuRef = useRef<HTMLDivElement>(null);
  const { user } = useAppSelector((state) => state.auth);
  const dispatch = useAppDispatch();
  const navigate = useNavigate();

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
    navigate('/login');
  };

  return (
    <header className={styles.header}>
      <div className={styles.container}>
        <div className={styles.content}>
          <h1 className={styles.title}>Admin Dashboard</h1>

          <div className={styles.rightSection}>
            {/* Notifications */}
            <button className={styles.iconButton} aria-label="Notifications">
              <BellIcon className={styles.icon} />
              <span className={styles.notificationBadge}>3</span>
            </button>

            {/* User Menu */}
            <div className={styles.userMenuWrapper} ref={userMenuRef}>
              <button
                onClick={() => setUserMenuOpen(!userMenuOpen)}
                className={styles.userButton}
                aria-expanded={userMenuOpen}
                aria-label="User menu"
              >
                <div className={styles.userAvatar}>{user?.firstName?.charAt(0).toUpperCase() || 'A'}</div>
                <span className={styles.userName}>{user?.firstName || 'Admin'}</span>
                <ChevronDownIcon className={styles.dropdownIcon} />
              </button>

              {userMenuOpen && (
                <div className={styles.dropdownMenu}>
                  <div className={styles.dropdownHeader}>
                    <p className={styles.dropdownHeaderLabel}>Logged in as</p>
                    <p className={styles.dropdownHeaderName}>{user?.firstName} {user?.lastName}</p>
                    <p className={styles.dropdownHeaderEmail}>{user?.email}</p>
                  </div>
                  <div className={styles.dropdownContent}>
                    <button className={styles.dropdownItem}>
                      <SettingsIcon />
                      Profile
                    </button>
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
          </div>
        </div>
      </div>
    </header>
  );
}
