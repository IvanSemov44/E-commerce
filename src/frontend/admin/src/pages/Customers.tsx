import { Card, CardContent, CardHeader, CardTitle } from '../components/ui/Card';
import Input from '../components/ui/Input';
import styles from './PageTemplate.module.css';

export default function Customers() {
  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <h1 className={styles.title}>Customers</h1>
        <Input placeholder="Search customers..." />
      </div>

      <Card variant="elevated">
        <CardHeader>
          <CardTitle>Customer List</CardTitle>
        </CardHeader>
        <CardContent>
          <p className={styles.placeholder}>Customers table will be displayed here</p>
        </CardContent>
      </Card>
    </div>
  );
}
