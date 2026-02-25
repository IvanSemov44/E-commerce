import { useState } from 'react';
import { Link } from 'react-router-dom';
import styles from './AnnouncementBar.module.css';

interface AnnouncementBarProps {
  message?: string;
  link?: string;
  linkText?: string;
  dismissible?: boolean;
}

export default function AnnouncementBar({
  message = "Free shipping on orders over $50!",
  link = "/products",
  linkText = "Shop now",
  dismissible = true,
}: AnnouncementBarProps) {
  const [isVisible, setIsVisible] = useState(true);

  if (!isVisible) return null;

  return (
    <div className={styles.announcementBar}>
      <div className={styles.content}>
        <span className={styles.message}>{message}</span>
        {link && linkText && (
          <Link to={link} className={styles.link}>
            {linkText}
          </Link>
        )}
      </div>
      {dismissible && (
        <button
          className={styles.dismissButton}
          onClick={() => setIsVisible(false)}
          aria-label="Dismiss announcement"
        >
          ×
        </button>
      )}
    </div>
  );
}
