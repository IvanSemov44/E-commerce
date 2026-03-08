import { useState } from 'react';
import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useToast } from '@/shared/components/Toast';
import {
  FacebookIcon,
  TwitterIcon,
  InstagramIcon,
  LinkedInIcon,
  YouTubeIcon,
} from '@/shared/components/icons';
import styles from './Footer.module.css';

const socialLinks = [
  { icon: <FacebookIcon />, href: '#', label: 'Facebook' },
  { icon: <TwitterIcon />, href: '#', label: 'Twitter' },
  { icon: <InstagramIcon />, href: '#', label: 'Instagram' },
  { icon: <LinkedInIcon />, href: '#', label: 'LinkedIn' },
  { icon: <YouTubeIcon />, href: '#', label: 'YouTube' },
];

export default function Footer() {
  const { t } = useTranslation();
  const [email, setEmail] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);
  const { success, error } = useToast();

  const handleNewsletterSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!email || !email.includes('@')) {
      error(t('footer.emailInvalid'));
      return;
    }

    setIsSubmitting(true);

    try {
      // Simulate API call - in production, this would be a real newsletter subscription
      await new Promise((resolve) => setTimeout(resolve, 1000));

      // Store in localStorage as a simple demo
      const subscribers = JSON.parse(localStorage.getItem('newsletter_subscribers') || '[]');
      if (!subscribers.includes(email)) {
        subscribers.push(email);
        localStorage.setItem('newsletter_subscribers', JSON.stringify(subscribers));
        success(t('footer.subscribeSuccess'));
      } else {
        error(t('footer.emailAlreadySubscribed'));
      }

      setEmail('');
    } catch {
      error(t('footer.subscribeFailed'));
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <footer className={styles.footer}>
      <div className={styles.container}>
        <div className={styles.grid}>
          {/* Company */}
          <div className={styles.section}>
            <h3>{t('footer.company')}</h3>
            <ul className={styles.links}>
              <li>
                <Link to="/about">{t('footer.aboutUs')}</Link>
              </li>
              <li>
                <Link to="/careers">{t('footer.careers')}</Link>
              </li>
              <li>
                <Link to="/press">{t('footer.press')}</Link>
              </li>
              <li>
                <Link to="/blog">{t('footer.blog')}</Link>
              </li>
            </ul>
          </div>

          {/* Support */}
          <div className={styles.section}>
            <h3>{t('footer.support')}</h3>
            <ul className={styles.links}>
              <li>
                <Link to="/help">{t('footer.helpCenter')}</Link>
              </li>
              <li>
                <Link to="/contact">{t('footer.contactUs')}</Link>
              </li>
              <li>
                <Link to="/track-order">{t('footer.trackOrder')}</Link>
              </li>
              <li>
                <Link to="/returns">{t('footer.returns')}</Link>
              </li>
            </ul>
          </div>

          {/* Legal */}
          <div className={styles.section}>
            <h3>Legal</h3>
            <ul className={styles.links}>
              <li>
                <Link to="/privacy">{t('footer.privacyPolicy')}</Link>
              </li>
              <li>
                <Link to="/terms">{t('footer.termsOfService')}</Link>
              </li>
              <li>
                <Link to="/cookies">{t('footer.cookiePolicy')}</Link>
              </li>
              <li>
                <Link to="/security">{t('footer.security')}</Link>
              </li>
            </ul>
          </div>

          {/* Newsletter */}
          <div className={styles.newsletter}>
            <h3>{t('footer.newsletter')}</h3>
            <p className={styles.newsletterText}>{t('footer.newsletterSubtitle')}</p>
            <form className={styles.form} onSubmit={handleNewsletterSubmit}>
              <input
                type="email"
                placeholder={t('footer.yourEmail')}
                className={styles.emailInput}
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                disabled={isSubmitting}
              />
              <button type="submit" className={styles.subscribeButton} disabled={isSubmitting}>
                {isSubmitting ? t('footer.subscribing') : t('footer.subscribe')}
              </button>
            </form>
          </div>
        </div>

        {/* Bottom */}
        <div className={styles.bottom}>
          <p className={styles.copyright}>&copy; 2026 E-Commerce. {t('footer.copyright')}</p>
          <div className={styles.socialLinks}>
            {socialLinks.map((social, index) => (
              <a
                key={index}
                href={social.href}
                className={styles.socialLink}
                aria-label={social.label}
                title={social.label}
              >
                {social.icon}
              </a>
            ))}
          </div>
        </div>
      </div>
    </footer>
  );
}
