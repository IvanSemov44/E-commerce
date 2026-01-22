import { useParams, Link, useNavigate } from 'react-router-dom';
import { useGetOrderByIdQuery, useCancelOrderMutation } from '../store/api/ordersApi';
import Button from '../components/ui/Button';
import Card from '../components/ui/Card';
import ErrorAlert from '../components/ErrorAlert';
import LoadingSkeleton from '../components/LoadingSkeleton';

export default function OrderDetail() {
  const navigate = useNavigate();
  const { orderId } = useParams<{ orderId: string }>();
  const { data: order, isLoading, error } = useGetOrderByIdQuery(orderId || '', {
    skip: !orderId,
  });

  const [cancelOrder, { isLoading: isCancelling }] = useCancelOrderMutation();

  if (!orderId) {
    return (
      <div style={{ maxWidth: '1200px', margin: '0 auto', padding: '0 1rem' }}>
        <ErrorAlert message="Order not found" />
      </div>
    );
  }

  const handleCancel = async () => {
    if (
      window.confirm(
        'Are you sure you want to cancel this order? This action cannot be undone.'
      )
    ) {
      try {
        await cancelOrder(orderId).unwrap();
        navigate('/orders');
      } catch (err) {
        alert('Failed to cancel order');
      }
    }
  };

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

  const canCancel =
    order &&
    (order.status === 'Pending' || order.status === 'Processing');

  return (
    <div style={{ maxWidth: '1000px', margin: '0 auto', padding: '0 1rem' }}>
      <div style={{ marginBottom: '2rem' }}>
        <Link to="/orders" style={{ textDecoration: 'none' }}>
          <Button variant="secondary">← Back to Orders</Button>
        </Link>
      </div>

      {error ? (
        <ErrorAlert message="Failed to load order details. Please try again later." />
      ) : isLoading ? (
        <LoadingSkeleton count={1} type="card" />
      ) : order ? (
        <div style={{ display: 'grid', gap: '2rem' }}>
          {/* Order Header */}
          <Card variant="elevated" padding="lg">
            <div
              style={{
                display: 'grid',
                gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))',
                gap: '2rem',
              }}
            >
              <div>
                <p style={{ margin: 0, fontSize: '0.875rem', color: '#666' }}>
                  Order Number
                </p>
                <p
                  style={{
                    margin: '0.5rem 0 0 0',
                    fontSize: '1.25rem',
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
                    margin: '0.5rem 0 0 0',
                    fontSize: '1rem',
                    fontWeight: 500,
                  }}
                >
                  {new Date(order.createdAt).toLocaleDateString()}{' '}
                  {new Date(order.createdAt).toLocaleTimeString()}
                </p>
              </div>

              <div>
                <p style={{ margin: 0, fontSize: '0.875rem', color: '#666' }}>
                  Status
                </p>
                <p
                  style={{
                    margin: '0.5rem 0 0 0',
                    fontSize: '1rem',
                    fontWeight: 600,
                    color: getStatusColor(order.status),
                  }}
                >
                  {order.status}
                </p>
              </div>

              {canCancel && (
                <div>
                  <Button
                    variant="secondary"
                    onClick={handleCancel}
                    disabled={isCancelling}
                  >
                    Cancel Order
                  </Button>
                </div>
              )}
            </div>
          </Card>

          {/* Order Items */}
          <Card variant="elevated" padding="lg">
            <h2 style={{ marginTop: 0 }}>Order Items</h2>

            <div
              style={{
                display: 'grid',
                gap: '1rem',
                borderTop: '1px solid #e0e0e0',
                paddingTop: '1rem',
              }}
            >
              {order.items.map((item, index) => (
                <div
                  key={index}
                  style={{
                    display: 'grid',
                    gridTemplateColumns: 'auto 1fr auto auto',
                    gap: '1rem',
                    alignItems: 'center',
                    paddingBottom: '1rem',
                    borderBottom:
                      index < order.items.length - 1
                        ? '1px solid #e0e0e0'
                        : 'none',
                  }}
                >
                  {item.productImageUrl && (
                    <img
                      src={item.productImageUrl}
                      alt={item.productName}
                      style={{
                        width: '60px',
                        height: '60px',
                        objectFit: 'cover',
                        borderRadius: '0.5rem',
                      }}
                    />
                  )}

                  <div>
                    <p
                      style={{
                        margin: 0,
                        fontWeight: 600,
                      }}
                    >
                      {item.productName}
                    </p>
                    <p
                      style={{
                        margin: '0.25rem 0 0 0',
                        fontSize: '0.875rem',
                        color: '#666',
                      }}
                    >
                      Quantity: {item.quantity}
                    </p>
                  </div>

                  <div style={{ textAlign: 'right' }}>
                    <p style={{ margin: 0, fontSize: '0.875rem', color: '#666' }}>
                      Price
                    </p>
                    <p style={{ margin: '0.25rem 0 0 0', fontWeight: 600 }}>
                      ${item.unitPrice?.toFixed(2) || '0.00'}
                    </p>
                  </div>

                  <div style={{ textAlign: 'right' }}>
                    <p style={{ margin: 0, fontSize: '0.875rem', color: '#666' }}>
                      Total
                    </p>
                    <p
                      style={{
                        margin: '0.25rem 0 0 0',
                        fontWeight: 600,
                        fontSize: '1.025rem',
                        color: '#1976d2',
                      }}
                    >
                      ${item.totalPrice?.toFixed(2) || '0.00'}
                    </p>
                  </div>
                </div>
              ))}
            </div>
          </Card>

          {/* Order Totals */}
          <Card variant="elevated" padding="lg">
            <div style={{ display: 'grid', gridTemplateColumns: '1fr auto', gap: '1rem' }}>
              <div />

              <div style={{ minWidth: '250px' }}>
                <div style={{ display: 'grid', gap: '1rem' }}>
                  <div
                    style={{
                      display: 'grid',
                      gridTemplateColumns: '1fr auto',
                      gap: '1rem',
                    }}
                  >
                    <span>Subtotal:</span>
                    <span>${order.subtotal?.toFixed(2) || '0.00'}</span>
                  </div>

                  {order.discountAmount && order.discountAmount > 0 && (
                    <div
                      style={{
                        display: 'grid',
                        gridTemplateColumns: '1fr auto',
                        gap: '1rem',
                        color: '#4caf50',
                      }}
                    >
                      <span>Discount:</span>
                      <span>-${order.discountAmount.toFixed(2)}</span>
                    </div>
                  )}

                  <div
                    style={{
                      display: 'grid',
                      gridTemplateColumns: '1fr auto',
                      gap: '1rem',
                    }}
                  >
                    <span>Shipping:</span>
                    <span>${order.shippingAmount?.toFixed(2) || '0.00'}</span>
                  </div>

                  <div
                    style={{
                      display: 'grid',
                      gridTemplateColumns: '1fr auto',
                      gap: '1rem',
                    }}
                  >
                    <span>Tax:</span>
                    <span>${order.taxAmount?.toFixed(2) || '0.00'}</span>
                  </div>

                  <div
                    style={{
                      display: 'grid',
                      gridTemplateColumns: '1fr auto',
                      gap: '1rem',
                      fontSize: '1.125rem',
                      fontWeight: 600,
                      borderTop: '1px solid #e0e0e0',
                      paddingTop: '1rem',
                      color: '#1976d2',
                    }}
                  >
                    <span>Total:</span>
                    <span>${order.totalAmount?.toFixed(2) || '0.00'}</span>
                  </div>
                </div>
              </div>
            </div>
          </Card>

          {/* Shipping Address */}
          <Card variant="elevated" padding="lg">
            <h2 style={{ marginTop: 0 }}>Shipping Address</h2>

            <div style={{ color: '#333' }}>
              <p style={{ margin: 0 }}>
                {order.shippingAddress?.firstName} {order.shippingAddress?.lastName}
              </p>
              <p style={{ margin: '0.25rem 0 0 0' }}>
                {order.shippingAddress?.streetLine1}
              </p>
              <p style={{ margin: '0.25rem 0 0 0' }}>
                {order.shippingAddress?.city}, {order.shippingAddress?.state}{' '}
                {order.shippingAddress?.postalCode}
              </p>
              <p style={{ margin: '0.25rem 0 0 0' }}>
                {order.shippingAddress?.country}
              </p>
              <p style={{ margin: '0.25rem 0 0 0', color: '#666' }}>
                Phone: {order.shippingAddress?.phone}
              </p>
            </div>
          </Card>
        </div>
      ) : null}
    </div>
  );
}
