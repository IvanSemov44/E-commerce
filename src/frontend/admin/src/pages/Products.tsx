import { Card, CardContent, CardHeader, CardTitle } from '../components/ui/Card';
import Button from '../components/ui/Button';
import styles from './PageTemplate.module.css';

export default function Products() {
  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <h1 className={styles.title}>Products</h1>
        <Button>Add Product</Button>
      </div>

      <Card variant="elevated">
        <CardHeader>
          <CardTitle>Products List</CardTitle>
        </CardHeader>
        <CardContent>
          <p className={styles.placeholder}>Products list will be displayed here</p>
        </CardContent>
      </Card>
    </div>
  );
}
