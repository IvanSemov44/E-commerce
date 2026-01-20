import { Card, CardContent, CardHeader, CardTitle } from '../components/ui/Card';
import Input from '../components/ui/Input';
import styles from './PageTemplate.module.css';

export default function Orders() {
  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <h1 className={styles.title}>Orders</h1>
        <Input placeholder="Search orders..." />
      </div>

      <Card variant="elevated">
        <CardHeader>
          <CardTitle>All Orders</CardTitle>
        </CardHeader>
        <CardContent>
          <p className={styles.placeholder}>Orders table will be displayed here</p>
        </CardContent>
      </Card>
    </div>
  );
}
