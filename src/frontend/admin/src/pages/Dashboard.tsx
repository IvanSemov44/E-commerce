import { Card, CardContent, CardHeader, CardTitle } from '../components/ui/Card';
import styles from './Dashboard.module.css';

export default function Dashboard() {
  // Mock data
  const stats = [
    { label: 'Total Orders', value: '1,234', change: '+12.5%', icon: '🛒' },
    { label: 'Total Revenue', value: '$12,345', change: '+8.2%', icon: '💰' },
    { label: 'Total Customers', value: '456', change: '+5.3%', icon: '👥' },
    { label: 'Total Products', value: '89', change: '+2.1%', icon: '📦' },
  ];

  return (
    <div className={styles.container}>
      <h1 className={styles.title}>Dashboard</h1>

      {/* Stats Grid */}
      <div className={styles.statsGrid}>
        {stats.map((stat) => (
          <Card key={stat.label} variant="elevated">
            <CardContent className={styles.statContent}>
              <div className={styles.statIcon}>{stat.icon}</div>
              <div className={styles.statText}>
                <p className={styles.statLabel}>{stat.label}</p>
                <p className={styles.statValue}>{stat.value}</p>
                <p className={styles.statChange}>{stat.change}</p>
              </div>
            </CardContent>
          </Card>
        ))}
      </div>

      {/* Charts and Tables */}
      <div className={styles.chartsGrid}>
        <Card variant="elevated">
          <CardHeader>
            <CardTitle>Recent Orders</CardTitle>
          </CardHeader>
          <CardContent>
            <p className={styles.placeholder}>Orders chart will be displayed here</p>
          </CardContent>
        </Card>

        <Card variant="elevated">
          <CardHeader>
            <CardTitle>Revenue</CardTitle>
          </CardHeader>
          <CardContent>
            <p className={styles.placeholder}>Revenue chart will be displayed here</p>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
