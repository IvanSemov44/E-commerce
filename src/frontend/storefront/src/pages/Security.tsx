import { Link } from 'react-router-dom';
import Card from '@/shared/components/ui/Card';
import styles from './LegalPage.module.css';

export default function Security() {
  return (
    <div className={styles.container}>
      <Card variant="elevated" padding="lg" className={styles.card}>
        <h1 className={styles.title}>Security</h1>
        <p className={styles.lastUpdated}>How we protect your information</p>

        <section className={styles.section}>
          <h2>Our Commitment to Security</h2>
          <p>
            Your security is our top priority. We implement industry-leading security measures to
            protect your personal information, payment details, and shopping experience. This page
            outlines the security practices we follow.
          </p>
        </section>

        <section className={styles.section}>
          <h2>Data Encryption</h2>
          <ul>
            <li>
              <strong>SSL/TLS Encryption:</strong> All data transmitted between your browser and our
              servers is encrypted using industry-standard SSL/TLS protocols
            </li>
            <li>
              <strong>Data at Rest:</strong> Sensitive data stored in our systems is encrypted using
              AES-256 encryption
            </li>
            <li>
              <strong>Secure Sessions:</strong> Your login sessions are protected with secure,
              encrypted tokens
            </li>
          </ul>
        </section>

        <section className={styles.section}>
          <h2>Payment Security</h2>
          <ul>
            <li>
              <strong>PCI DSS Compliance:</strong> We comply with Payment Card Industry Data
              Security Standards
            </li>
            <li>
              <strong>Tokenization:</strong> We never store your full credit card number – payment
              details are handled by certified payment processors
            </li>
            <li>
              <strong>Fraud Detection:</strong> Advanced fraud detection systems monitor
              transactions for suspicious activity
            </li>
            <li>
              <strong>3D Secure:</strong> We support 3D Secure authentication for additional payment
              protection
            </li>
          </ul>
        </section>

        <section className={styles.section}>
          <h2>Account Security</h2>
          <ul>
            <li>
              <strong>Password Protection:</strong> Passwords are hashed using bcrypt and never
              stored in plain text
            </li>
            <li>
              <strong>Two-Factor Authentication:</strong> Optional 2FA adds an extra layer of
              security to your account
            </li>
            <li>
              <strong>Session Management:</strong> Automatic logout after periods of inactivity
            </li>
            <li>
              <strong>Login Notifications:</strong> Email alerts for new device logins
            </li>
          </ul>
        </section>

        <section className={styles.section}>
          <h2>Infrastructure Security</h2>
          <ul>
            <li>
              <strong>Regular Security Audits:</strong> Third-party security assessments and
              penetration testing
            </li>
            <li>
              <strong>Firewall Protection:</strong> Web application firewalls protect against common
              attacks
            </li>
            <li>
              <strong>Regular Updates:</strong> Systems and dependencies are kept up to date with
              security patches
            </li>
            <li>
              <strong>Access Controls:</strong> Strict access controls limit who can access customer
              data
            </li>
          </ul>
        </section>

        <section className={styles.section}>
          <h2>Report a Security Issue</h2>
          <p>
            If you discover a security vulnerability, please report it responsibly:
            <br />
            <br />
            Email: security@ecommerce.com
            <br />
            <br />
            We appreciate responsible disclosure and will investigate all reports promptly.
          </p>
        </section>

        <section className={styles.section}>
          <h2>Security Tips for Customers</h2>
          <ul>
            <li>Use a strong, unique password for your account</li>
            <li>Enable two-factor authentication when available</li>
            <li>Be cautious of phishing emails – we never ask for your password via email</li>
            <li>Always verify you're on our official website before entering credentials</li>
            <li>Keep your browser and operating system updated</li>
          </ul>
        </section>

        <div className={styles.backLink}>
          <Link to="/">← Back to Home</Link>
        </div>
      </Card>
    </div>
  );
}
