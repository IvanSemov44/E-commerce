import { Link } from 'react-router';
import { ROUTE_PATHS } from '@/shared/constants/navigation';
import Card from '@/shared/components/ui/Card';
import styles from './LegalPage.module.css';

export default function CookiePolicy() {
  return (
    <div className={styles.container}>
      <Card variant="elevated" padding="lg" className={styles.card}>
        <h1 className={styles.title}>Cookie Policy</h1>
        <p className={styles.lastUpdated}>Last Updated: February 24, 2026</p>

        <section className={styles.section}>
          <h2>1. What Are Cookies</h2>
          <p>
            Cookies are small text files that are stored on your computer or mobile device when you
            visit our website. They are widely used to make websites work more efficiently and
            provide information to website owners.
          </p>
        </section>

        <section className={styles.section}>
          <h2>2. How We Use Cookies</h2>
          <p>We use cookies for the following purposes:</p>
          <ul>
            <li>
              <strong>Essential Cookies:</strong> Required for the website to function properly
              (authentication, shopping cart, checkout)
            </li>
            <li>
              <strong>Analytics Cookies:</strong> Help us understand how visitors interact with our
              website
            </li>
            <li>
              <strong>Marketing Cookies:</strong> Used to deliver relevant advertisements and track
              campaign effectiveness
            </li>
            <li>
              <strong>Preference Cookies:</strong> Remember your settings and preferences for a
              better experience
            </li>
          </ul>
        </section>

        <section className={styles.section}>
          <h2>3. Types of Cookies We Use</h2>
          <h3>Session Cookies</h3>
          <p>
            These are temporary cookies that are deleted when you close your browser. They are used
            to maintain your session during a single visit.
          </p>

          <h3>Persistent Cookies</h3>
          <p>
            These remain on your device for a set period or until you delete them. They remember
            your preferences for future visits.
          </p>

          <h3>Third-Party Cookies</h3>
          <p>
            Set by third-party services we use (analytics, payment processors, social media). These
            are subject to those third parties' privacy policies.
          </p>
        </section>

        <section className={styles.section}>
          <h2>4. Managing Cookies</h2>
          <p>You can control and manage cookies in various ways:</p>
          <ul>
            <li>Use our cookie consent banner to accept or reject non-essential cookies</li>
            <li>Configure your browser settings to block or delete cookies</li>
            <li>Use browser extensions for more granular cookie control</li>
          </ul>
          <p className="mt-4">
            <strong>Note:</strong> Blocking essential cookies may affect the functionality of our
            website, particularly the shopping cart and checkout process.
          </p>
        </section>

        <section className={styles.section}>
          <h2>5. Browser Cookie Settings</h2>
          <p>
            Most browsers allow you to manage cookie settings. Here's how to access these settings
            in popular browsers:
          </p>
          <ul>
            <li>
              <strong>Chrome:</strong> Settings → Privacy and Security → Cookies
            </li>
            <li>
              <strong>Firefox:</strong> Settings → Privacy & Security → Cookies
            </li>
            <li>
              <strong>Safari:</strong> Preferences → Privacy → Cookies
            </li>
            <li>
              <strong>Edge:</strong> Settings → Cookies and Site Permissions
            </li>
          </ul>
        </section>

        <section className={styles.section}>
          <h2>6. Contact Us</h2>
          <p>
            If you have questions about our use of cookies, please contact us at:
            <br />
            <br />
            Email: privacy@ecommerce.com
            <br />
            Phone: +1 (555) 123-4567
          </p>
        </section>

        <div className={styles.backLink}>
          <Link to={ROUTE_PATHS.home}>← Back to Home</Link>
        </div>
      </Card>
    </div>
  );
}
