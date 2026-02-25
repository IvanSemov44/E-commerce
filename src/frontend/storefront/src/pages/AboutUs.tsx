import { Link } from 'react-router-dom';
import Card from '../components/ui/Card';
import styles from './LegalPage.module.css';

export default function AboutUs() {
  return (
    <div className={styles.container}>
      <Card variant="elevated" padding="lg" className={styles.card}>
        <h1 className={styles.title}>About Us</h1>
        <p className={styles.lastUpdated}>Learn more about our journey and mission</p>
        
        <section className={styles.section}>
          <h2>Our Story</h2>
          <p>
            Founded in 2024, we started with a simple mission: to make online shopping 
            accessible, enjoyable, and trustworthy for everyone. What began as a small 
            team of passionate entrepreneurs has grown into a thriving e-commerce platform 
            serving customers worldwide.
          </p>
        </section>

        <section className={styles.section}>
          <h2>Our Mission</h2>
          <p>
            We believe that everyone deserves access to quality products at fair prices, 
            delivered with exceptional service. Our mission is to:
          </p>
          <ul>
            <li>Provide a curated selection of high-quality products</li>
            <li>Offer competitive pricing without compromising on quality</li>
            <li>Deliver an exceptional shopping experience from browse to delivery</li>
            <li>Build lasting relationships with our customers based on trust</li>
          </ul>
        </section>

        <section className={styles.section}>
          <h2>Our Values</h2>
          <ul>
            <li><strong>Customer First:</strong> Every decision we make starts with our customers in mind</li>
            <li><strong>Quality:</strong> We partner only with trusted suppliers and brands</li>
            <li><strong>Transparency:</strong> No hidden fees, no surprises – just honest business</li>
            <li><strong>Sustainability:</strong> We're committed to reducing our environmental impact</li>
            <li><strong>Innovation:</strong> Constantly improving our platform to serve you better</li>
          </ul>
        </section>

        <section className={styles.section}>
          <h2>Our Team</h2>
          <p>
            Our diverse team brings together expertise in technology, retail, logistics, 
            and customer service. United by our passion for e-commerce, we work tirelessly 
            to ensure your shopping experience exceeds expectations.
          </p>
        </section>

        <section className={styles.section}>
          <h2>Contact Us</h2>
          <p>
            Have questions or feedback? We'd love to hear from you!
            <br /><br />
            Email: hello@ecommerce.com<br />
            Phone: +1 (555) 123-4567<br />
            Address: 123 Commerce Street, Business City, BC 12345
          </p>
        </section>

        <div className={styles.backLink}>
          <Link to="/">← Back to Home</Link>
        </div>
      </Card>
    </div>
  );
}
