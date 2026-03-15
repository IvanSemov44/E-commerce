import { Link } from 'react-router';
import { ROUTE_PATHS } from '@/shared/constants/navigation';
import { Card } from '@/shared/components/ui/Card';
import styles from './LegalPage.module.css';

export default function Press() {
  return (
    <div className={styles.container}>
      <Card variant="elevated" padding="lg" className={styles.card}>
        <h1 className={styles.title}>Press & Media</h1>
        <p className={styles.lastUpdated}>News, media resources, and press inquiries</p>

        <section className={styles.section}>
          <h2>About Our Company</h2>
          <p>
            Founded in 2024, we've grown from a small startup to a leading e-commerce platform
            serving customers worldwide. Our mission is to make online shopping accessible,
            enjoyable, and trustworthy for everyone.
          </p>
        </section>

        <section className={styles.section}>
          <h2>Press Releases</h2>
          <ul>
            <li>
              <strong>February 2026:</strong> Launch of new mobile app with enhanced features
            </li>
            <li>
              <strong>January 2026:</strong> Expansion to 10 new international markets
            </li>
            <li>
              <strong>December 2025:</strong> Record holiday season with 200% growth
            </li>
            <li>
              <strong>October 2025:</strong> Partnership announcement with major logistics provider
            </li>
          </ul>
        </section>

        <section className={styles.section}>
          <h2>Company Facts</h2>
          <ul>
            <li>
              <strong>Founded:</strong> 2024
            </li>
            <li>
              <strong>Headquarters:</strong> Business City, BC
            </li>
            <li>
              <strong>Employees:</strong> 150+ worldwide
            </li>
            <li>
              <strong>Customers:</strong> 500,000+ served
            </li>
            <li>
              <strong>Products:</strong> 10,000+ items
            </li>
            <li>
              <strong>Markets:</strong> 25+ countries
            </li>
          </ul>
        </section>

        <section className={styles.section}>
          <h2>Media Contact</h2>
          <p>
            For press inquiries, interview requests, or media resources:
            <br />
            <br />
            Email: press@ecommerce.com
            <br />
            Phone: +1 (555) 123-4567
            <br />
            Response time: Within 24 hours on business days
          </p>
        </section>

        <section className={styles.section}>
          <h2>Media Resources</h2>
          <p>
            High-resolution logos, product images, and executive headshots are available upon
            request. Please contact our press team with your specific needs.
          </p>
        </section>

        <div className={styles.backLink}>
          <Link to={ROUTE_PATHS.home}>Back to Home</Link>
        </div>
      </Card>
    </div>
  );
}
