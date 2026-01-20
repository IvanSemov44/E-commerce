import { Card, CardContent, CardHeader, CardTitle } from '../components/ui/Card';
import Input from '../components/ui/Input';
import Button from '../components/ui/Button';
import styles from './PageTemplate.module.css';

export default function Settings() {
  return (
    <div className={styles.container}>
      <h1 className={styles.title}>Settings</h1>

      <Card variant="elevated">
        <CardHeader>
          <CardTitle>Store Settings</CardTitle>
        </CardHeader>
        <CardContent className={styles.settingsForm}>
          <Input label="Store Name" placeholder="Enter store name" />
          <Input label="Email" placeholder="Enter email" />
          <Input label="Phone" placeholder="Enter phone" />
          <div className={styles.formActions}>
            <Button variant="outline">Cancel</Button>
            <Button>Save Changes</Button>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
