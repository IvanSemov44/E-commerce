import { useParams, Link, useNavigate } from 'react-router';
import { useTranslation } from 'react-i18next';
import { useGetOrderByIdQuery, useCancelOrderMutation } from '@/features/orders/api/ordersApi';
import { useApiErrorHandler } from '@/shared/hooks';
import { ROUTE_PATHS } from '@/shared/constants/navigation';
import { Button } from '@/shared/components/ui/Button';
import { ErrorAlert } from '@/shared/components/ErrorAlert';
import { OrderTotals } from '@/shared/components';
import {
  OrderHeader,
  OrderItemsList,
  ShippingAddress,
  OrderDetailSkeleton,
} from '@/features/orders/components';
import styles from './OrderDetailPage.module.css';

export default function OrderDetailPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const { orderId } = useParams<{ orderId: string }>();
  const {
    data: order,
    isLoading,
    error,
  } = useGetOrderByIdQuery(orderId || '', {
    skip: !orderId,
  });
  const { handleError } = useApiErrorHandler();

  const [cancelOrder, { isLoading: isCancelling }] = useCancelOrderMutation();

  if (!orderId) {
    return (
      <div className={styles.container}>
        <ErrorAlert message={t('orders.orderNotFound')} />
      </div>
    );
  }

  const handleCancel = async () => {
    if (window.confirm(t('orders.cancelConfirmMessage'))) {
      try {
        await cancelOrder(orderId).unwrap();
        navigate(ROUTE_PATHS.orders);
      } catch (err) {
        handleError(err, t('orders.failedToCancel'));
      }
    }
  };

  const canCancel: boolean = !!(
    order &&
    (order.status === 'Pending' || order.status === 'Processing')
  );

  return (
    <div className={styles.container}>
      <div className={styles.backButton}>
        <Link to={ROUTE_PATHS.orders} className={styles.backLink}>
          <Button variant="secondary">{t('orders.backToOrders')}</Button>
        </Link>
      </div>

      {error ? (
        <ErrorAlert message={t('orders.failedToLoadOrder')} />
      ) : isLoading ? (
        <OrderDetailSkeleton />
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

          {order.trackingNumber && (
            <div className="mt-6 p-4 bg-purple-50 border border-purple-200 rounded-lg">
              <p className="text-sm font-medium text-purple-700 mb-1">
                {t('orders.trackingNumber')}
              </p>
              <p className="text-base font-semibold text-purple-900 font-mono">
                {order.trackingNumber}
              </p>
            </div>
          )}
        </div>
      ) : null}
    </div>
  );
}
