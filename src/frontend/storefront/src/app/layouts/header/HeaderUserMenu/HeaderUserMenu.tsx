import { Link } from 'react-router';
import { useRef, useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import { useAppSelector } from '@/shared/lib/store';
import { selectCurrentUser } from '@/features/auth/slices/authSlice';
import { ROUTE_PATHS } from '@/shared/constants/navigation';
import { ChevronDownIcon, UserIcon, LogoutIcon } from '@/shared/components/icons';
import styles from './HeaderUserMenu.module.css';

interface HeaderUserMenuProps {
  isOpen: boolean;
  onToggle: () => void;
  onClose: () => void;
  onLogout: () => void;
}

export default function HeaderUserMenu({
  isOpen,
  onToggle,
  onClose,
  onLogout,
}: HeaderUserMenuProps) {
  const user = useAppSelector(selectCurrentUser);
  const { t } = useTranslation();
  const menuRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!isOpen) {
      return;
    }

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
        aria-label={t('nav.userMenu')}
      >
        <div className={styles.userAvatar}>{user?.firstName?.charAt(0).toUpperCase() ?? 'U'}</div>
        <span className={styles.userName}>{user?.firstName}</span>
        <ChevronDownIcon className={styles.dropdownIcon} />
      </button>

      {isOpen && (
        <div className={styles.dropdownMenu}>
          <div className={styles.dropdownHeader}>
            <p className={styles.dropdownHeaderLabel}>{t('nav.account')}</p>
            <p className={styles.dropdownHeaderName}>{user?.firstName}</p>
            <p className={styles.dropdownHeaderEmail}>{user?.email}</p>
          </div>
          <div className={styles.dropdownContent}>
            <Link to={ROUTE_PATHS.profile} onClick={onClose} className={styles.dropdownItem}>
              <UserIcon className={styles.dropdownIcon} />
              {t('nav.myProfile')}
            </Link>
            <button onClick={onLogout} className={styles.dropdownItem}>
              <LogoutIcon />
              {t('nav.signOut')}
            </button>
          </div>
        </div>
      )}
    </div>
  );
}
