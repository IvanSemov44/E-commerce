import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { FOOTER_SECTIONS } from '@/shared/constants/navigation';
import { FOOTER_SOCIAL_LINKS } from './socialLinks';
import { useNewsletterSubscription } from './useNewsletterSubscription';
import styles from '../Footer.module.css';

export default function Footer() {
  const { t } = useTranslation();
  const { email, isSubmitting, setEmail, handleNewsletterSubmit } = useNewsletterSubscription({
    invalidEmailMessage: t('footer.emailInvalid'),
    subscribeSuccessMessage: t('footer.subscribeSuccess'),
    alreadySubscribedMessage: t('footer.emailAlreadySubscribed'),
    subscribeFailedMessage: t('footer.subscribeFailed'),
  });

  return (
    <footer className={styles.footer}>
      <div className={styles.container}>
        <div className={styles.grid}>
          {FOOTER_SECTIONS.map((section) => (
            <div key={section.title} className={styles.section}>
              <h3>{section.title.startsWith('footer.') ? t(section.title) : section.title}</h3>
              <ul className={styles.links}>
                {section.links.map((link) => (
                  <li key={link.path}>
                    <Link to={link.path}>{t(link.labelKey)}</Link>
                  </li>
                ))}
              </ul>
            </div>
          ))}

          <div className={styles.newsletter}>
            <h3>{t('footer.newsletter')}</h3>
            <p className={styles.newsletterText}>{t('footer.newsletterSubtitle')}</p>
            <form className={styles.form} onSubmit={handleNewsletterSubmit}>
              <input
                type="email"
                placeholder={t('footer.yourEmail')}
                className={styles.emailInput}
                value={email}
                onChange={(event) => setEmail(event.target.value)}
                disabled={isSubmitting}
              />
              <button type="submit" className={styles.subscribeButton} disabled={isSubmitting}>
                {isSubmitting ? t('footer.subscribing') : t('footer.subscribe')}
              </button>
            </form>
          </div>
        </div>

        <div className={styles.bottom}>
          <p className={styles.copyright}>&copy; 2026 E-Commerce. {t('footer.copyright')}</p>
          <div className={styles.socialLinks}>
            {FOOTER_SOCIAL_LINKS.map((social, index) => (
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
