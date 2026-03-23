import { Link } from 'react-router';
import { ROUTE_PATHS } from '@/shared/constants/navigation';
import { Card } from '@/shared/components/ui/Card';
import styles from './LegalPage.module.css';

export function PrivacyPolicy() {
  return (
    <div className={styles.container}>
      <Card variant="elevated" padding="lg" className={styles.card}>
        <h1 className={styles.title}>Privacy Policy</h1>
        <p className={styles.lastUpdated}>Last Updated: February 24, 2026</p>

        <section className={styles.section}>
          <h2>1. Introduction</h2>
          <p>
            Welcome to our e-commerce platform. We respect your privacy and are committed to
            protecting your personal data. This privacy policy will inform you about how we look
            after your personal data when you visit our website and tell you about your privacy
            rights and how the law protects you.
          </p>
        </section>

        <section className={styles.section}>
          <h2>2. Data We Collect</h2>
          <p>
            We may collect, use, store and transfer different kinds of personal data about you,
            including:
          </p>
          <ul>
            <li>
              <strong>Identity Data:</strong> First name, last name, username or similar identifier
            </li>
            <li>
              <strong>Contact Data:</strong> Email address, telephone numbers, billing and shipping
              addresses
            </li>
            <li>
              <strong>Financial Data:</strong> Payment card details (processed securely by our
              payment providers)
            </li>
            <li>
              <strong>Transaction Data:</strong> Details about payments to and from you and other
              details of products you have purchased
            </li>
            <li>
              <strong>Technical Data:</strong> IP address, browser type and version, time zone
              setting, browser plug-in types and versions
            </li>
            <li>
              <strong>Usage Data:</strong> Information about how you use our website, products and
              services
            </li>
          </ul>
        </section>

        <section className={styles.section}>
          <h2>3. How We Use Your Data</h2>
          <p>
            We will only use your personal data when the law allows us to. Most commonly, we will
            use your personal data in the following circumstances:
          </p>
          <ul>
            <li>To process and deliver your order including managing payments and delivery</li>
            <li>To manage your account and maintain your relationship with us</li>
            <li>To improve our website, products, and services</li>
            <li>To send you marketing communications (with your consent)</li>
            <li>To comply with legal obligations</li>
          </ul>
        </section>

        <section className={styles.section}>
          <h2>4. Data Security</h2>
          <p>
            We have put in place appropriate security measures to prevent your personal data from
            being accidentally lost, used or accessed in an unauthorized way, altered or disclosed.
            In addition, we limit access to your personal data to those employees, agents,
            contractors and other third parties who have a business need to know.
          </p>
        </section>

        <section className={styles.section}>
          <h2>5. Cookies</h2>
          <p>
            Our website uses cookies to distinguish you from other users of our website. This helps
            us to provide you with a good experience when you browse our website and also allows us
            to improve our site. For detailed information on the cookies we use and the purposes for
            which we use them, please see our{' '}
            <Link to={ROUTE_PATHS.cookies} className={styles.link}>
              Cookie Policy
            </Link>
            .
          </p>
        </section>

        <section className={styles.section}>
          <h2>6. Your Legal Rights</h2>
          <p>
            Under certain circumstances, you have rights under data protection laws in relation to
            your personal data, including the right to:
          </p>
          <ul>
            <li>Request access to your personal data</li>
            <li>Request correction of your personal data</li>
            <li>Request erasure of your personal data</li>
            <li>Object to processing of your personal data</li>
            <li>Request restriction of processing your personal data</li>
            <li>Request transfer of your personal data</li>
            <li>Right to withdraw consent</li>
          </ul>
        </section>

        <section className={styles.section}>
          <h2>7. Contact Us</h2>
          <p>
            If you have any questions about this privacy policy or our privacy practices, please
            contact us at:
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
