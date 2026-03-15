import { Link } from 'react-router';
import { ROUTE_PATHS } from '@/shared/constants/navigation';
import Card from '@/shared/components/ui/Card';
import styles from './LegalPage.module.css';

export default function TrackOrder() {
  return (
    <div className={styles.container}>
      <Card variant="elevated" padding="lg" className={styles.card}>
        <h1 className={styles.title}>Track Your Order</h1>
        <p className={styles.lastUpdated}>Check the status of your shipment</p>

        <section className={styles.section}>
          <h2>Order Tracking</h2>
          <p>
            Enter your order number and email address to track your package. You can find your order
            number in your confirmation email or in your account's order history.
          </p>

          <div className="page-note">
            <p className="page-note-text">
              <strong>Note:</strong> If you have an account, you can view all your orders and their
              current status in the{' '}
              <Link to={ROUTE_PATHS.orders} className={styles.link}>
                Order History
              </Link>{' '}
              section.
            </p>
          </div>
        </section>

        <section className={styles.section}>
          <h2>Order Status Meanings</h2>
          <ul>
            <li>
              <strong>Pending:</strong> Order received, awaiting payment confirmation
            </li>
            <li>
              <strong>Processing:</strong> Payment confirmed, order is being prepared
            </li>
            <li>
              <strong>Shipped:</strong> Order has left our warehouse and is on its way
            </li>
            <li>
              <strong>Delivered:</strong> Package has been delivered to the shipping address
            </li>
            <li>
              <strong>Cancelled:</strong> Order was cancelled before shipping
            </li>
          </ul>
        </section>

        <section className={styles.section}>
          <h2>Shipping Partners</h2>
          <p>We work with trusted shipping partners to ensure your package arrives safely:</p>
          <ul>
            <li>UPS</li>
            <li>FedEx</li>
            <li>USPS</li>
            <li>DHL (International)</li>
          </ul>
        </section>

        <section className={styles.section}>
          <h2>Need Help?</h2>
          <p>
            If you're having trouble tracking your order or have questions about delivery:
            <br />
            <br />
            Email: support@ecommerce.com
            <br />
            Phone: +1 (555) 123-4567
            <br />
            Please have your order number ready for faster assistance.
          </p>
        </section>

        <div className={styles.backLink}>
          <Link to={ROUTE_PATHS.home}>← Back to Home</Link>
        </div>
      </Card>
    </div>
  );
}
