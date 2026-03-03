import { Link } from 'react-router-dom';
import Card from '@/shared/components/ui/Card';
import styles from './LegalPage.module.css';

export default function Contact() {
  return (
    <div className={styles.container}>
      <Card variant="elevated" padding="lg" className={styles.card}>
        <h1 className={styles.title}>Contact Us</h1>
        <p className={styles.lastUpdated}>We're here to help</p>
        
        <section className={styles.section}>
          <h2>Get in Touch</h2>
          <p>
            Have a question, feedback, or need assistance? Our customer support team is 
            available to help you with any inquiries. Choose the best way to reach us below.
          </p>
        </section>

        <section className={styles.section}>
          <h2>Contact Information</h2>
          <ul>
            <li><strong>Email:</strong> support@ecommerce.com</li>
            <li><strong>Phone:</strong> +1 (555) 123-4567</li>
            <li><strong>Hours:</strong> Monday - Friday, 9:00 AM - 6:00 PM EST</li>
            <li><strong>Address:</strong> 123 Commerce Street, Business City, BC 12345</li>
          </ul>
        </section>

        <section className={styles.section}>
          <h2>Department Contacts</h2>
          <ul>
            <li><strong>Customer Support:</strong> support@ecommerce.com</li>
            <li><strong>Sales Inquiries:</strong> sales@ecommerce.com</li>
            <li><strong>Partnership Opportunities:</strong> partners@ecommerce.com</li>
            <li><strong>Press & Media:</strong> press@ecommerce.com</li>
            <li><strong>Careers:</strong> careers@ecommerce.com</li>
          </ul>
        </section>

        <section className={styles.section}>
          <h2>Response Time</h2>
          <p>
            We aim to respond to all inquiries within 24-48 business hours. For urgent 
            matters, please call our customer support line for immediate assistance.
          </p>
        </section>

        <section className={styles.section}>
          <h2>Office Location</h2>
          <p>
            Our headquarters is located in the heart of Business City. While we primarily 
            operate remotely, we welcome visitors by appointment.
          </p>
        </section>

        <div className={styles.backLink}>
          <Link to="/">← Back to Home</Link>
        </div>
      </Card>
    </div>
  );
}

