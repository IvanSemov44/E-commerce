/* eslint-disable max-lines-per-function */
import { Link } from 'react-router';
import { ROUTE_PATHS } from '@/shared/constants/navigation';
import Card from '@/shared/components/ui/Card';
import styles from './LegalPage.module.css';

export default function TermsOfService() {
  return (
    <div className={styles.container}>
      <Card variant="elevated" padding="lg" className={styles.card}>
        <h1 className={styles.title}>Terms of Service</h1>
        <p className={styles.lastUpdated}>Last Updated: February 24, 2026</p>

        <section className={styles.section}>
          <h2>1. Agreement to Terms</h2>
          <p>
            These Terms of Service constitute a legally binding agreement made between you, whether
            personally or on behalf of an entity ("you") and our e-commerce platform ("Company,"
            "we," "us," or "our"), concerning your access to and use of our website as well as any
            other media form, media channel, mobile website or mobile application related, linked,
            or otherwise connected thereto (collectively, the "Site").
          </p>
          <p>
            You agree that by accessing the Site, you have read, understood, and agreed to be bound
            by all of these Terms of Service. IF YOU DO NOT AGREE WITH ALL OF THESE TERMS OF
            SERVICE, THEN YOU ARE EXPRESSLY PROHIBITED FROM USING THE SITE AND YOU MUST DISCONTINUE
            USE IMMEDIATELY.
          </p>
        </section>

        <section className={styles.section}>
          <h2>2. User Accounts</h2>
          <p>
            When you create an account with us, you must provide us information that is accurate,
            complete, and current at all times. Failure to do so constitutes a breach of the Terms,
            which may result in immediate termination of your account on our Site.
          </p>
          <p>
            You are responsible for safeguarding the password that you use to access the Site and
            for any activities or actions under your password.
          </p>
          <p>
            We may suspend or terminate your account at any time for any reason at our sole
            discretion.
          </p>
        </section>

        <section className={styles.section}>
          <h2>3. Products and Services</h2>
          <p>
            All descriptions of products or product pricing are subject to change at any time
            without notice, at the sole discretion of us. We reserve the right to discontinue any
            product at any time. Any offer for any product or service made on the Site is void where
            prohibited.
          </p>
          <p>
            We do not warrant that the quality of any products, services, information, or other
            material purchased or obtained by you will meet your expectations, or that any errors in
            the Service will be corrected.
          </p>
        </section>

        <section className={styles.section}>
          <h2>4. Purchases and Payment</h2>
          <p>We accept the following forms of payment:</p>
          <ul>
            <li>Visa, Mastercard, American Express, and other major credit cards</li>
            <li>PayPal</li>
            <li>Apple Pay and Google Pay</li>
          </ul>
          <p>
            You agree to provide current, complete, and accurate purchase and account information
            for all purchases made via the Site. You further agree to promptly update account and
            payment information, including email address, payment method, and payment card
            expiration date, so that we can complete your transactions and contact you as needed.
          </p>
        </section>

        <section className={styles.section}>
          <h2>5. Shipping and Delivery</h2>
          <p>
            We offer various shipping options which will be presented to you during the checkout
            process. Delivery times are estimates and are not guaranteed. We are not responsible for
            delays caused by shipping carriers or customs processing.
          </p>
          <p>
            Risk of loss and title for items purchased from this Site pass to you upon delivery of
            the items to the carrier. You are responsible for filing any claims with carriers for
            damaged and/or lost shipments.
          </p>
        </section>

        <section className={styles.section}>
          <h2>6. Returns and Refunds</h2>
          <p>
            Our return policy allows returns within 30 days of purchase. Items must be unused, in
            original packaging, and accompanied by proof of purchase. For full details, please see
            our
            <Link to={ROUTE_PATHS.returns} className={styles.link}>
              {' '}
              Returns Policy
            </Link>
            .
          </p>
          <p>
            Refunds will be processed within 5-10 business days after we receive and inspect the
            returned item. The refund will be credited to the original payment method.
          </p>
        </section>

        <section className={styles.section}>
          <h2>7. Intellectual Property</h2>
          <p>
            The Site and its original content, features and functionality are and will remain the
            exclusive property of the Company and its licensors. The Site is protected by copyright,
            trademark, and other laws of both the Company and foreign countries.
          </p>
          <p>
            Our trademarks and trade dress may not be used in connection with any product or service
            without the prior written consent of the Company.
          </p>
        </section>

        <section className={styles.section}>
          <h2>8. Prohibited Activities</h2>
          <p>
            You may not access or use the Site for any purpose other than that for which we make the
            Site available. Prohibited activities include:
          </p>
          <ul>
            <li>
              Using the Site in any way that violates any applicable federal, state, local, or
              international law or regulation
            </li>
            <li>Using the Site for any unlawful or illegal purpose</li>
            <li>
              Attempting to gain unauthorized access to any part of the Site or any systems or
              networks connected to the Site
            </li>
            <li>
              Interfering with or disrupting the Site or servers or networks connected to the Site
            </li>
            <li>
              Collecting or harvesting any information or data from the Site without our consent
            </li>
          </ul>
        </section>

        <section className={styles.section}>
          <h2>9. Limitation of Liability</h2>
          <p>
            IN NO EVENT WILL WE OR OUR DIRECTORS, EMPLOYEES, OR AGENTS BE LIABLE TO YOU OR ANY THIRD
            PARTY FOR ANY DIRECT, INDIRECT, CONSEQUENTIAL, EXEMPLARY, INCIDENTAL, SPECIAL, OR
            PUNITIVE DAMAGES, INCLUDING LOST PROFIT, LOST REVENUE, LOSS OF DATA, OR OTHER DAMAGES
            ARISING FROM YOUR USE OF THE SITE, EVEN IF WE HAVE BEEN ADVISED OF THE POSSIBILITY OF
            SUCH DAMAGES.
          </p>
        </section>

        <section className={styles.section}>
          <h2>10. Governing Law</h2>
          <p>
            These Terms shall be governed by and defined following the laws of the jurisdiction in
            which the Company is registered. The Company's ability to seek injunctive or other
            equitable relief in any court of competent jurisdiction is not affected by this
            provision.
          </p>
        </section>

        <section className={styles.section}>
          <h2>11. Changes to Terms</h2>
          <p>
            We reserve the right, in our sole discretion, to make changes or modifications to these
            Terms of Service at any time and for any reason. We will alert you about any changes by
            updating the "Last Updated" date of these Terms of Service, and you waive any right to
            receive specific notice of each such change.
          </p>
        </section>

        <section className={styles.section}>
          <h2>12. Contact Information</h2>
          <p>
            If you have any questions about these Terms, please contact us at:
            <br />
            <br />
            Email: legal@ecommerce.com
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
