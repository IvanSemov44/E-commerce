import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useGetOrdersQuery } from '@/features/orders/api/ordersApi';
import Button from '@/shared/components/ui/Button';
import PageHeader from '@/shared/components/PageHeader';
import QueryRenderer from '@/shared/components/QueryRenderer';
import { PackageIcon, DocumentIcon } from '@/shared/components/icons';
import { OrderCard } from '@/features/orders/components';
import styles from './OrderHistoryPage.module.css';

interface OrderForDisplay {
  id: string;
  orderNumber: string;
  status: string;
  totalAmount: number;
  createdAt: string;
  items: Array<{ productName: string }>;
}

export default function OrderHistoryPage() {
  const { t } = useTranslation();
  const { data: ordersData, isLoading, error } = useGetOrdersQuery();
  const orders: OrderForDisplay[] = (ordersData || []).map((order) => ({
    id: order.id,
    orderNumber: order.orderNumber,
    status: order.status,
    totalAmount: order.totalAmount,
    createdAt: order.createdAt,
    items: order.items || [],
  }));

  return (
    <div className={styles.container}>
      <PageHeader 
        title={t('orders.title')} 
        subtitle={t('orders.subtitle')}
        icon={<PackageIcon />}
        badge={t('account.myOrders')}
      />

      <QueryRenderer
        isLoading={isLoading}
        error={error}
        data={orders}
        errorMessage={t('orders.failedToLoadOrders')}
        emptyState={{
          icon: (
            <DocumentIcon className={styles.emptyIcon} />
          ),
          title: t('orders.noOrdersYet'),
          description: t('account.startShopping'),
          action: (
            <Link to="/products">
              <Button>{t('account.browseProducts')}</Button>
            </Link>
          ),
        }}
      >
        {(orders) => (
          <div className={styles.ordersList}>
            {orders.map((order) => (
              <OrderCard key={order.id} order={order} />
            ))}
          </div>
        )}
      </QueryRenderer>
    </div>
  );
}
