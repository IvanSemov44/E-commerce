/* eslint-disable max-lines-per-function */
import { Link } from 'react-router';
import { ROUTE_PATHS } from '@/shared/constants/navigation';
import { Card } from '@/shared/components/ui/Card';
import styles from './LegalPage.module.css';

export default function ReturnsPolicy() {
  return (
    <div className={styles.container}>
      <Card variant="elevated" padding="lg" className={styles.card}>
        <h1 className={styles.title}>Returns Policy</h1>
        <p className={styles.lastUpdated}>Last Updated: February 24, 2026</p>

        <section className={styles.section}>
          <h2>Our Return Policy</h2>
          <p>
            We want you to be completely satisfied with your purchase. If you're not happy with your
            order, we offer a straightforward returns policy to make the process as easy as
            possible.
          </p>
        </section>

        <section className={styles.section}>
          <h2>Return Window</h2>
          <p>
            You have <strong>30 days</strong> from the date of delivery to return an item. After
            this period, unfortunately we cannot offer you a refund or exchange.
          </p>
        </section>

        <section className={styles.section}>
          <h2>Eligibility for Returns</h2>
          <p>To be eligible for a return, your item must be:</p>
          <ul>
            <li>Unused and in the same condition that you received it</li>
            <li>In its original packaging</li>
            <li>Accompanied by the original receipt or proof of purchase</li>
            <li>With all tags still attached</li>
          </ul>
          <p>
            <strong>Non-returnable items include:</strong>
          </p>
          <ul>
            <li>Gift cards</li>
            <li>Downloadable software products</li>
            <li>Personalized or custom-made items</li>
            <li>Intimate or sanitary goods</li>
            <li>Hazardous materials, or flammable liquids or gases</li>
          </ul>
        </section>

        <section className={styles.section}>
          <h2>How to Initiate a Return</h2>
          <p>To start a return, follow these steps:</p>
          <ol>
            <li>
              Log into your account and go to your{' '}
              <Link to={ROUTE_PATHS.orders} className={styles.link}>
                Order History
              </Link>
            </li>
            <li>Select the order containing the item(s) you wish to return</li>
            <li>Click "Request Return" and select the items to return</li>
            <li>Choose your reason for return from the dropdown menu</li>
            <li>Submit your request</li>
          </ol>
          <p>
            Alternatively, you can contact our customer service team at returns@ecommerce.com with
            your order number and details of the items you wish to return.
          </p>
        </section>

        <section className={styles.section}>
          <h2>Return Shipping</h2>
          <p>
            <strong>Free returns:</strong> We provide free return shipping for defective items or if
            we sent you the wrong product.
          </p>
          <p>
            <strong>Standard returns:</strong> For all other returns, you will be responsible for
            paying the shipping costs. Shipping costs are non-refundable. If you receive a refund,
            the cost of return shipping will be deducted from your refund.
          </p>
          <p>
            We recommend using a trackable shipping service and purchasing shipping insurance. We
            cannot guarantee that we will receive your returned item.
          </p>
        </section>

        <section className={styles.section}>
          <h2>Refund Process</h2>
          <p>
            Once your return is received and inspected, we will send you an email to notify you that
            we have received your returned item.
          </p>
          <p>
            <strong>If approved:</strong> Your refund will be processed, and a credit will
            automatically be applied to your credit card or original method of payment within 5-10
            business days.
          </p>
          <p>
            <strong>If rejected:</strong> We will notify you of the reason for rejection and the
            item will be returned to you at your expense.
          </p>
        </section>

        <section className={styles.section}>
          <h2>Late or Missing Refunds</h2>
          <p>If you haven't received a refund yet, first check your bank account again.</p>
          <p>
            Next, contact your credit card company. It may take some time before your refund is
            officially posted.
          </p>
          <p>
            If you've done all of this and you still have not received your refund yet, please
            contact us at support@ecommerce.com.
          </p>
        </section>

        <section className={styles.section}>
          <h2>Exchanges</h2>
          <p>
            We only replace items if they are defective or damaged. If you need to exchange a
            defective or damaged item for the same item, send us an email at exchanges@ecommerce.com
            with your order number and photos of the defect or damage.
          </p>
          <p>
            If you would like to exchange an item for a different size or color, you will need to
            return the original item and place a new order.
          </p>
        </section>

        <section className={styles.section}>
          <h2>Sale Items</h2>
          <p>
            Only regular-priced items may be refunded. Sale items cannot be refunded unless they are
            defective or damaged.
          </p>
        </section>

        <section className={styles.section}>
          <h2>Damaged or Defective Items</h2>
          <p>
            If you received a damaged or defective item, please contact us immediately at
            support@ecommerce.com with your order number and photos of the damage or defect. We will
            arrange for a replacement or full refund at no additional cost to you.
          </p>
        </section>

        <section className={styles.section}>
          <h2>International Returns</h2>
          <p>
            For international orders, the customer is responsible for all return shipping costs,
            customs duties, and taxes. We recommend using a shipping method that provides tracking
            and insurance.
          </p>
        </section>

        <section className={styles.section}>
          <h2>Contact Us</h2>
          <p>
            If you have any questions about our Returns Policy, please contact us:
            <br />
            <br />
            Email: returns@ecommerce.com
            <br />
            Phone: +1 (555) 123-4567
            <br />
            Hours: Monday - Friday, 9:00 AM - 5:00 PM EST
          </p>
        </section>

        <div className={styles.backLink}>
          <Link to={ROUTE_PATHS.home}>← Back to Home</Link>
        </div>
      </Card>
    </div>
  );
}
