import { Link } from 'react-router';
import { ROUTE_PATHS } from '@/shared/constants/navigation';
import { Card } from '@/shared/components/ui/Card';
import styles from './LegalPage.module.css';

export default function Blog() {
  return (
    <div className={styles.container}>
      <Card variant="elevated" padding="lg" className={styles.card}>
        <h1 className={styles.title}>Blog</h1>
        <p className={styles.lastUpdated}>Insights, news, and stories from our team</p>

        <section className={styles.section}>
          <h2>Latest Articles</h2>
          <p>
            Stay up to date with the latest trends in e-commerce, product spotlights, company news,
            and helpful guides for getting the most out of your shopping experience.
          </p>
        </section>

        <section className={styles.section}>
          <h2>Categories</h2>
          <ul>
            <li>
              <strong>Product Spotlights:</strong> Deep dives into our latest products and
              collections
            </li>
            <li>
              <strong>Shopping Guides:</strong> Tips and tricks for smart online shopping
            </li>
            <li>
              <strong>Company News:</strong> Updates and announcements from our team
            </li>
            <li>
              <strong>Industry Insights:</strong> Trends and developments in e-commerce
            </li>
            <li>
              <strong>Customer Stories:</strong> Real experiences from our community
            </li>
          </ul>
        </section>

        <section className={styles.section}>
          <h2>Featured Posts</h2>
          <ul>
            <li>
              <strong>10 Tips for a Better Online Shopping Experience</strong>
              <br />
              <span>February 20, 2026</span>
            </li>
            <li>
              <strong>Behind the Scenes: How We Ensure Product Quality</strong>
              <br />
              <span>February 15, 2026</span>
            </li>
            <li>
              <strong>Sustainable Shopping: Making Eco-Friendly Choices</strong>
              <br />
              <span>February 10, 2026</span>
            </li>
          </ul>
        </section>

        <section className={styles.section}>
          <h2>Subscribe</h2>
          <p>
            Want to receive our latest articles directly in your inbox? Subscribe to our newsletter
            using the form in the footer. We send updates once a week with our best content,
            exclusive deals, and early access to new products.
          </p>
        </section>

        <section className={styles.section}>
          <h2>Write for Us</h2>
          <p>
            Interested in contributing to our blog? We welcome guest posts from industry experts,
            influencers, and passionate customers. Contact us at:
            <br />
            <br />
            Email: blog@ecommerce.com
          </p>
        </section>

        <div className={styles.backLink}>
          <Link to={ROUTE_PATHS.home}>← Back to Home</Link>
        </div>
      </Card>
    </div>
  );
}
