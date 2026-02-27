import { Link } from 'react-router-dom';
import Card from '../components/ui/Card';
import styles from './LegalPage.module.css';

export default function Careers() {
  return (
    <div className={styles.container}>
      <Card variant="elevated" padding="lg" className={styles.card}>
        <h1 className={styles.title}>Careers</h1>
        <p className={styles.lastUpdated}>Join our growing team</p>
        
        <section className={styles.section}>
          <h2>Why Work With Us?</h2>
          <p>
            We're always looking for talented individuals who share our passion for 
            e-commerce and customer experience. When you join our team, you become 
            part of a dynamic, innovative company that values creativity, collaboration, 
            and personal growth.
          </p>
        </section>

        <section className={styles.section}>
          <h2>Our Culture</h2>
          <ul>
            <li><strong>Remote-First:</strong> Work from anywhere with flexible hours</li>
            <li><strong>Learning & Development:</strong> Continuous learning opportunities and education stipend</li>
            <li><strong>Health & Wellness:</strong> Comprehensive health coverage and wellness programs</li>
            <li><strong>Team Events:</strong> Regular team building activities and company retreats</li>
            <li><strong>Growth Opportunities:</strong> Clear career progression paths</li>
          </ul>
        </section>

        <section className={styles.section}>
          <h2>Open Positions</h2>
          <p>
            We're currently hiring for the following roles:
          </p>
          <ul>
            <li>Senior Frontend Developer (React/TypeScript)</li>
            <li>Backend Developer (.NET/C#)</li>
            <li>DevOps Engineer</li>
            <li>Product Manager</li>
            <li>Customer Success Specialist</li>
            <li>Marketing Coordinator</li>
          </ul>
          <p className="mt-4">
            Don't see a position that fits? We're always interested in hearing from 
            talented individuals. Send your resume and cover letter to careers@ecommerce.com
          </p>
        </section>

        <section className={styles.section}>
          <h2>Benefits</h2>
          <ul>
            <li>Competitive salary and equity options</li>
            <li>Flexible PTO policy</li>
            <li>Health, dental, and vision insurance</li>
            <li>401(k) matching</li>
            <li>Home office setup stipend</li>
            <li>Employee discount on all products</li>
          </ul>
        </section>

        <section className={styles.section}>
          <h2>How to Apply</h2>
          <p>
            Ready to join our team? Send your resume and cover letter to:
            <br /><br />
            Email: careers@ecommerce.com<br />
            Subject: [Position Name] Application
          </p>
        </section>

        <div className={styles.backLink}>
          <Link to="/">Back to Home</Link>
        </div>
      </Card>
    </div>
  );
}