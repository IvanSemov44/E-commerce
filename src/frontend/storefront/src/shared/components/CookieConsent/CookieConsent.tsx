import { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import styles from './CookieConsent.module.css';

const COOKIE_CONSENT_KEY = 'cookie-consent';

export default function CookieConsent() {
  const [isVisible, setIsVisible] = useState(false);

  useEffect(() => {
    // Check if user has already made a choice
    const consent = localStorage.getItem(COOKIE_CONSENT_KEY);
    if (!consent) {
      // Small delay to prevent flash on page load
      const timer = setTimeout(() => setIsVisible(true), 1000);
      return () => clearTimeout(timer);
    }
  }, []);

  const handleAccept = () => {
    localStorage.setItem(COOKIE_CONSENT_KEY, 'accepted');
    setIsVisible(false);
  };

  const handleDecline = () => {
    localStorage.setItem(COOKIE_CONSENT_KEY, 'declined');
    setIsVisible(false);
  };

  if (!isVisible) {
    return null;
  }

  return (
    <div className={styles.overlay}>
      <div className={styles.banner}>
        <div className={styles.content}>
          <h3 className={styles.title}>🍪 Cookie Preferences</h3>
          <p className={styles.description}>
            We use cookies to enhance your browsing experience, serve personalized ads or content,
            and analyze our traffic. By clicking "Accept All", you consent to our use of cookies.
            Read more in our{' '}
            <Link to="/privacy" className={styles.link}>
              Privacy Policy
            </Link>
            .
          </p>
        </div>
        <div className={styles.actions}>
          <button onClick={handleDecline} className={styles.declineButton}>
            Decline
          </button>
          <button onClick={handleAccept} className={styles.acceptButton}>
            Accept All
          </button>
        </div>
      </div>
    </div>
  );
}
