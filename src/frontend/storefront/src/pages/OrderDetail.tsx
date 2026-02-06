import { useParams, Link, useNavigate } from 'react-router-dom';
import toast from 'react-hot-toast';
import { useGetOrderByIdQuery, useCancelOrderMutation } from '../store/api/ordersApi';
import Button from '../components/ui/Button';
import ErrorAlert from '../components/ErrorAlert';
import LoadingSkeleton from '../components/LoadingSkeleton';
import { OrderHeader, OrderItemsList, OrderTotals, ShippingAddress } from './components/OrderDetail';
import styles from './OrderDetail.module.css';

export default function OrderDetail() {
  const navigate = useNavigate();
  const { orderId } = useParams<{ orderId: string }>();
  const { data: order, isLoading, error } = useGetOrderByIdQuery(orderId || '', {
    skip: !orderId,
  });

  const [cancelOrder, { isLoading: isCancelling }] = useCancelOrderMutation();

  if (!orderId) {
    return (
      <div className={styles.container}>
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
        toast.error('Failed to cancel order');
      }
    }
  };

  const canCancel: boolean = !!(order && (order.status === 'Pending' || order.status === 'Processing'));

  return (
    <div className={styles.container}>
      <div className={styles.backButton}>
        <Link to="/orders" className={styles.backLink}>
          <Button variant="secondary">← Back to Orders</Button>
        </Link>
      </div>

      {error ? (
        <ErrorAlert message="Failed to load order details. Please try again later." />
      ) : isLoading ? (
        <LoadingSkeleton count={1} type="card" />
      ) : order ? (
        <div className={styles.content}>
          <OrderHeader
            orderNumber={order.orderNumber}
            createdAt={order.createdAt}
            status={order.status}
            canCancel={canCancel}
            isCancelling={isCancelling}
            onCancel={handleCancel}
          />

          <OrderItemsList items={order.items} />

          <OrderTotals
            subtotal={order.subtotal}
            discountAmount={order.discountAmount}
            shippingAmount={order.shippingAmount}
            taxAmount={order.taxAmount}
            totalAmount={order.totalAmount}
          />

          <ShippingAddress address={order.shippingAddress} />
        </div>
      ) : null}
    </div>
  );
}
