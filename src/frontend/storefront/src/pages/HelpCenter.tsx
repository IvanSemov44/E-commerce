import { Link } from 'react-router';
import { ROUTE_PATHS } from '@/shared/constants/navigation';
import Card from '@/shared/components/ui/Card';
import styles from './LegalPage.module.css';

export default function HelpCenter() {
  return (
    <div className={styles.container}>
      <Card variant="elevated" padding="lg" className={styles.card}>
        <h1 className={styles.title}>Help Center</h1>
        <p className={styles.lastUpdated}>Find answers to common questions</p>

        <section className={styles.section}>
          <h2>Ordering & Payment</h2>
          <h3>How do I place an order?</h3>
          <p>
            Simply browse our products, add items to your cart, and proceed to checkout. You can pay
            with credit card, PayPal, or other available payment methods.
          </p>

          <h3>What payment methods do you accept?</h3>
          <p>
            We accept all major credit cards (Visa, MasterCard, American Express), PayPal, Apple
            Pay, and Google Pay.
          </p>

          <h3>Is my payment information secure?</h3>
          <p>
            Yes, we use industry-standard SSL encryption and never store your full credit card
            details. All payments are processed through secure, PCI-compliant payment processors.
          </p>
        </section>

        <section className={styles.section}>
          <h2>Shipping & Delivery</h2>
          <h3>How long does shipping take?</h3>
          <p>
            Standard shipping typically takes 5-7 business days. Express shipping is available for
            2-3 business day delivery. International shipping times vary by destination.
          </p>

          <h3>Do you offer free shipping?</h3>
          <p>Yes! We offer free standard shipping on all orders over $50.</p>

          <h3>How can I track my order?</h3>
          <p>
            Once your order ships, you'll receive a tracking number via email. You can also track
            your order in the "Order History" section of your account.
          </p>
        </section>

        <section className={styles.section}>
          <h2>Returns & Refunds</h2>
          <h3>What is your return policy?</h3>
          <p>
            We offer a 30-day return policy for most items. Products must be unused and in original
            packaging. See our{' '}
            <Link to={ROUTE_PATHS.returns} className={styles.link}>
              Returns Policy
            </Link>{' '}
            for full details.
          </p>

          <h3>How do I request a refund?</h3>
          <p>
            Contact our customer support team or initiate a return through your account. Refunds are
            typically processed within 5-7 business days after we receive the returned item.
          </p>
        </section>

        <section className={styles.section}>
          <h2>Account & Security</h2>
          <h3>How do I reset my password?</h3>
          <p>
            Click "Forgot Password" on the login page and enter your email address. You'll receive a
            link to reset your password.
          </p>

          <h3>How do I update my account information?</h3>
          <p>
            Log in to your account and visit the Profile section to update your personal
            information, addresses, and preferences.
          </p>
        </section>

        <section className={styles.section}>
          <h2>Still Need Help?</h2>
          <p>
            Can't find what you're looking for? Contact our support team:
            <br />
            <br />
            Email: support@ecommerce.com
            <br />
            Phone: +1 (555) 123-4567
            <br />
            Hours: Monday - Friday, 9:00 AM - 6:00 PM EST
          </p>
        </section>

        <div className={styles.backLink}>
          <Link to={ROUTE_PATHS.home}>← Back to Home</Link>
        </div>
      </Card>
    </div>
  );
}
