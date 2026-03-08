import { useState } from 'react';
import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import styles from './AnnouncementBar.module.css';

interface AnnouncementBarProps {
  message?: string;
  link?: string;
  linkText?: string;
  dismissible?: boolean;
}

export default function AnnouncementBar({
  message,
  link = '/products',
  linkText,
  dismissible = true,
}: AnnouncementBarProps) {
  const { t } = useTranslation();
  const [isVisible, setIsVisible] = useState(true);

  // Use provided message or translate default
  const displayMessage = message || t('announcement.freeShipping');
  const displayLinkText = linkText || t('announcement.shopNow');

  if (!isVisible) return null;

  return (
    <div className={styles.announcementBar}>
      <div className={styles.content}>
        <span className={styles.message}>{displayMessage}</span>
        {link && displayLinkText && (
          <Link to={link} className={styles.link}>
            {displayLinkText}
          </Link>
        )}
      </div>
      {dismissible && (
        <button
          className={styles.dismissButton}
          onClick={() => setIsVisible(false)}
          aria-label={t('announcement.dismiss')}
        >
          ×
        </button>
      )}
    </div>
  );
}
