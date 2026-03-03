import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useGetOrdersQuery } from '../../api/ordersApi';
import Button from '../../../../shared/components/ui/Button';
import PageHeader from '../../../../shared/components/PageHeader';
import QueryRenderer from '../../../../shared/components/QueryRenderer';
import { OrderCard } from '../../components';
import styles from './OrderHistoryPage.module.css';

// Icons
const PackageIcon = () => (
  <svg fill="none" stroke="currentColor" viewBox="0 0 24 24">
    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M20 7l-8-4-8 4m16 0l-8 4m8-4v10l-8 4m0-10L4 7m8 4v10M4 7v10l8 4" />
  </svg>
);

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
  const orders: OrderForDisplay[] = (ordersData || []).map((order: any) => ({
    id: order.id,
    orderNumber: order.orderNumber,
    status: order.status,
    totalAmount: order.totals?.total || order.totalAmount,
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
            <svg fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={2}
                d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"
              />
            </svg>
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
