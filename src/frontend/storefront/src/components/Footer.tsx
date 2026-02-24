import { Link } from 'react-router-dom';
import styles from './Footer.module.css';

export default function Footer() {
  return (
    <footer className={styles.footer}>
      <div className={styles.container}>
        <div className={styles.grid}>
          {/* Company */}
          <div className={styles.section}>
            <h3>Company</h3>
            <ul className={styles.links}>
              <li><Link to="/">About Us</Link></li>
              <li><Link to="/">Careers</Link></li>
              <li><Link to="/">Press</Link></li>
              <li><Link to="/">Blog</Link></li>
            </ul>
          </div>

          {/* Support */}
          <div className={styles.section}>
            <h3>Support</h3>
            <ul className={styles.links}>
              <li><Link to="/">Help Center</Link></li>
              <li><Link to="/">Contact Us</Link></li>
              <li><Link to="/">Track Order</Link></li>
              <li><Link to="/returns">Returns</Link></li>
            </ul>
          </div>

          {/* Legal */}
          <div className={styles.section}>
            <h3>Legal</h3>
            <ul className={styles.links}>
              <li><Link to="/privacy">Privacy Policy</Link></li>
              <li><Link to="/terms">Terms of Service</Link></li>
              <li><Link to="/">Cookies</Link></li>
              <li><Link to="/">Security</Link></li>
            </ul>
          </div>

          {/* Newsletter */}
          <div className={styles.newsletter}>
            <h3>Newsletter</h3>
            <p className={styles.newsletterText}>Subscribe to get special offers and updates</p>
            <form className={styles.form} onSubmit={(e) => e.preventDefault()}>
              <input
                type="email"
                placeholder="Your email"
                className={styles.emailInput}
              />
              <button className={styles.subscribeButton}>
                Subscribe
              </button>
            </form>
          </div>
        </div>

        {/* Bottom */}
        <div className={styles.bottom}>
          <p>&copy; 2026 E-Commerce. All rights reserved.</p>
          <div className={styles.socialLinks}>
            <a href="#">Facebook</a>
            <a href="#">Twitter</a>
            <a href="#">Instagram</a>
          </div>
        </div>
      </div>
    </footer>
  );
}
