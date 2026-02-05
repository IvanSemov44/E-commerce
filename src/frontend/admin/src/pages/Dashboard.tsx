import { Card, CardContent, CardHeader, CardTitle } from '../components/ui/Card';
import { useGetDashboardStatsQuery } from '../store/api/dashboardApi';
import styles from './Dashboard.module.css';

export default function Dashboard() {
  const { data: dashboardStats, isLoading, error } = useGetDashboardStatsQuery(undefined, {
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
      value: `$${dashboardStats!.totalRevenue.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`,
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
                  <li
                    key={trend.date}
                    style={{
                      padding: '0.5rem 0',
                      borderBottom: '1px solid #e2e8f0',
                      fontSize: '0.875rem',
                    }}
                  >
                    <span style={{ color: '#64748b' }}>
                      {new Date(trend.date).toLocaleDateString()}
                    </span>
                    : <strong>{trend.count}</strong> orders
                  </li>
                ))}
              </ul>
            ) : (
              <p style={{ color: '#94a3b8' }}>No trend data available</p>
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
                  <li
                    key={trend.date}
                    style={{
                      padding: '0.5rem 0',
                      borderBottom: '1px solid #e2e8f0',
                      fontSize: '0.875rem',
                    }}
                  >
                    <span style={{ color: '#64748b' }}>
                      {new Date(trend.date).toLocaleDateString()}
                    </span>
                    : <strong>${trend.amount.toFixed(2)}</strong>
                  </li>
                ))}
              </ul>
            ) : (
              <p style={{ color: '#94a3b8' }}>No trend data available</p>
            )}
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
