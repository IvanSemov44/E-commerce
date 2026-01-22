import { Link } from 'react-router-dom';
import { useGetOrdersQuery } from '../store/api/ordersApi';
import Button from '../components/ui/Button';
import Card from '../components/ui/Card';
import PageHeader from '../components/PageHeader';
import EmptyState from '../components/EmptyState';
import ErrorAlert from '../components/ErrorAlert';
import LoadingSkeleton from '../components/LoadingSkeleton';

export default function OrderHistory() {
  const { data: orders, isLoading, error } = useGetOrdersQuery();

  const getStatusColor = (status: string) => {
    switch (status) {
      case 'Pending':
        return '#ff9800';
      case 'Processing':
        return '#2196f3';
      case 'Shipped':
        return '#9c27b0';
      case 'Delivered':
        return '#4caf50';
      case 'Cancelled':
        return '#f44336';
      default:
        return '#666';
    }
  };

  return (
    <div style={{ maxWidth: '1200px', margin: '0 auto', padding: '0 1rem' }}>
      <PageHeader title="Order History" />

      {error ? (
        <ErrorAlert message="Failed to load orders. Please try again later." />
      ) : isLoading ? (
        <div style={{ display: 'grid', gap: '1rem' }}>
          <LoadingSkeleton count={3} type="card" />
        </div>
      ) : orders && orders.length > 0 ? (
        <div style={{ display: 'grid', gap: '1rem' }}>
          {orders.map((order) => (
            <Link
              key={order.id}
              to={`/orders/${order.id}`}
              style={{ textDecoration: 'none' }}
            >
              <Card
                variant="elevated"
                padding="lg"
                style={{
                  cursor: 'pointer',
                  transition: 'box-shadow 0.2s',
                }}
              >
                <div
                  style={{
                    display: 'grid',
                    gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))',
                    gap: '1rem',
                    alignItems: 'center',
                  }}
                >
                  <div>
                    <p style={{ margin: 0, fontSize: '0.875rem', color: '#666' }}>
                      Order Number
                    </p>
                    <p
                      style={{
                        margin: '0.25rem 0 0 0',
                        fontSize: '1.125rem',
                        fontWeight: 600,
                      }}
                    >
                      {order.orderNumber}
                    </p>
                  </div>

                  <div>
                    <p style={{ margin: 0, fontSize: '0.875rem', color: '#666' }}>
                      Date
                    </p>
                    <p
                      style={{
                        margin: '0.25rem 0 0 0',
                        fontSize: '1rem',
                        fontWeight: 500,
                      }}
                    >
                      {new Date(order.createdAt).toLocaleDateString()}
                    </p>
                  </div>

                  <div>
                    <p style={{ margin: 0, fontSize: '0.875rem', color: '#666' }}>
                      Status
                    </p>
                    <p
                      style={{
                        margin: '0.25rem 0 0 0',
                        fontSize: '1rem',
                        fontWeight: 600,
                        color: getStatusColor(order.status),
                      }}
                    >
                      {order.status}
                    </p>
                  </div>

                  <div>
                    <p style={{ margin: 0, fontSize: '0.875rem', color: '#666' }}>
                      Total
                    </p>
                    <p
                      style={{
                        margin: '0.25rem 0 0 0',
                        fontSize: '1.125rem',
                        fontWeight: 600,
                        color: '#1976d2',
                      }}
                    >
                      ${order.totals.total.toFixed(2)}
                    </p>
                  </div>

                  <div>
                    <p style={{ margin: 0, fontSize: '0.875rem', color: '#666' }}>
                      Items
                    </p>
                    <p
                      style={{
                        margin: '0.25rem 0 0 0',
                        fontSize: '1rem',
                        fontWeight: 500,
                      }}
                    >
                      {order.items.length} item{order.items.length !== 1 ? 's' : ''}
                    </p>
                  </div>

                  <div style={{ textAlign: 'right' }}>
                    <Button variant="secondary" size="sm">
                      View Details
                    </Button>
                  </div>
                </div>
              </Card>
            </Link>
          ))}
        </div>
      ) : (
        <EmptyState
          icon={
            <svg fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={2}
                d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"
              />
            </svg>
          }
          title="No orders yet"
          description="Start shopping to place your first order"
          action={
            <Link to="/products">
              <Button>Browse Products</Button>
            </Link>
          }
        />
      )}
    </div>
  );
}
