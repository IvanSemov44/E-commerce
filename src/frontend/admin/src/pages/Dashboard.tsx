import { Card, CardContent, CardHeader, CardTitle } from '../components/ui/Card';
import { useGetDashboardStatsQuery } from '../store/api/dashboardApi';
import styles from './Dashboard.module.css';
import { formatDate } from '../utils/formatters';

export default function Dashboard() {
  const {
    data: dashboardStats,
    isLoading,
    error,
  } = useGetDashboardStatsQuery(undefined, {
    pollingInterval: 30000, // Auto-refresh every 30 seconds
  });

  if (isLoading) {
    return (
      <div className={styles.container}>
        <h1 className={styles.title}>Dashboard</h1>
        <div className={styles.statsGrid}>
          {[1, 2, 3, 4].map((idx) => (
            <Card key={idx} variant="elevated">
              <CardContent className={styles.statContent}>
                <div className={styles.chartPlaceholder} />
              </CardContent>
            </Card>
          ))}
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className={styles.container}>
        <h1 className={styles.title}>Dashboard</h1>
        <Card variant="elevated">
          <CardContent>
            <p className={styles.errorText}>
              Failed to load dashboard statistics. Please try again.
            </p>
          </CardContent>
        </Card>
      </div>
    );
  }

  // Transform API data to stats array
  const stats = [
    {
      label: 'Total Orders',
      value: dashboardStats!.totalOrders.toLocaleString(),
      icon: '🛒',
    },
    {
      label: 'Total Revenue',
      value: `$${dashboardStats!.totalRevenue.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`, // keep locale string for large number formatting
      icon: '💰',
    },
    {
      label: 'Total Customers',
      value: dashboardStats!.totalCustomers.toLocaleString(),
      icon: '👥',
    },
    {
      label: 'Total Products',
      value: dashboardStats!.totalProducts.toLocaleString(),
      icon: '📦',
    },
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
              </div>
            </CardContent>
          </Card>
        ))}
      </div>

      {/* Charts and Tables */}
      <div className={styles.chartsGrid}>
        <Card variant="elevated">
          <CardHeader>
            <CardTitle>Orders Trend</CardTitle>
          </CardHeader>
          <CardContent>
            {dashboardStats!.ordersTrend && dashboardStats!.ordersTrend.length > 0 ? (
              <ul className={styles.trendList}>
                {dashboardStats!.ordersTrend.slice(0, 7).map((trend) => (
                  <li key={trend.date} className={styles.trendItem}>
                    <span className={styles.trendLabel}>{formatDate(trend.date)}</span>:{' '}
                    <strong>{trend.count}</strong> orders
                  </li>
                ))}
              </ul>
            ) : (
              <p className={styles.emptyText}>No trend data available</p>
            )}
          </CardContent>
        </Card>

        <Card variant="elevated">
          <CardHeader>
            <CardTitle>Revenue Trend</CardTitle>
          </CardHeader>
          <CardContent>
            {dashboardStats!.revenueTrend && dashboardStats!.revenueTrend.length > 0 ? (
              <ul className={styles.trendList}>
                {dashboardStats!.revenueTrend.slice(0, 7).map((trend) => (
                  <li key={trend.date} className={styles.trendItem}>
                    <span className={styles.trendLabel}>{formatDate(trend.date)}</span>:{' '}
                    <strong>${trend.amount.toFixed(2)}</strong>
                  </li>
                ))}
              </ul>
            ) : (
              <p className={styles.emptyText}>No trend data available</p>
            )}
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
