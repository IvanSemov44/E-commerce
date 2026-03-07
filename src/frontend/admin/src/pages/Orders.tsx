import { useState } from 'react';
import toast from 'react-hot-toast';
import { useGetOrdersQuery, useUpdateOrderStatusMutation } from '../store/api/ordersApi';
import { Card } from '../components/ui/Card';
import Input from '../components/ui/Input';
import Table from '../components/ui/Table';
import Badge from '../components/ui/Badge';
import Pagination from '../components/ui/Pagination';
import QueryRenderer from '../components/QueryRenderer';
import styles from './Orders.module.css';
import type { Order, OrderStatus } from '@shared/types';
import { formatCurrency, formatDate } from '../utils/formatters';

const statusColors: Record<OrderStatus, 'default' | 'info' | 'warning' | 'success' | 'error'> = {
  pending: 'warning',
  processing: 'info',
  shipped: 'info',
  delivered: 'success',
  cancelled: 'error',
};

// eslint-disable-next-line max-lines-per-function -- Orders CRUD page: inline column JSX with status selects, tracking input, and handlers
export default function Orders() {
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState('');
  const [statusFilter, setStatusFilter] = useState<OrderStatus | ''>('');

  const {
    data: ordersResult,
    isLoading,
    error,
  } = useGetOrdersQuery({
    page,
    pageSize: 20,
    search,
    status: statusFilter || undefined,
  });

  const [updateOrderStatus] = useUpdateOrderStatusMutation();

  const handleStatusChange = async (orderId: string, newStatus: OrderStatus) => {
    try {
      await updateOrderStatus({ orderId, status: newStatus }).unwrap();
      toast.success('Order status updated successfully');
    } catch {
      toast.error('Failed to update order status');
    }
  };

  const handleTrackingNumberUpdate = async (
    orderId: string,
    currentStatus: OrderStatus,
    trackingNumber: string
  ) => {
    try {
      await updateOrderStatus({ orderId, status: currentStatus, trackingNumber }).unwrap();
      toast.success('Tracking number updated');
    } catch {
      toast.error('Failed to update tracking number');
    }
  };

  const columns = [
    {
      header: 'Order #',
      accessor: (order: Order) => order.orderNumber,
      width: '12%',
    },
    {
      header: 'Customer',
      accessor: (order: Order) => order.userId,
      width: '15%',
    },
    {
      header: 'Total',
      accessor: (order: Order) => formatCurrency(order.totalAmount),
      width: '10%',
    },
    {
      header: 'Items',
      accessor: (order: Order) => order.items.length,
      width: '8%',
    },
    {
      header: 'Status',
      accessor: (order: Order) => (
        <Badge variant={statusColors[order.status]}>{order.status}</Badge>
      ),
      width: '12%',
    },
    {
      header: 'Payment',
      accessor: (order: Order) => (
        <Badge variant={order.paymentStatus === 'paid' ? 'success' : 'warning'}>
          {order.paymentStatus}
        </Badge>
      ),
      width: '12%',
    },
    {
      header: 'Date',
      accessor: (order: Order) => formatDate(order.createdAt),
      width: '12%',
    },
    {
      header: 'Actions',
      accessor: (order: Order) => (
        <div className={styles.actionsCell}>
          <select
            value={order.status}
            onChange={(e) => handleStatusChange(order.id, e.target.value as OrderStatus)}
            className={styles.statusSelect}
          >
            <option value="pending">Pending</option>
            <option value="processing">Processing</option>
            <option value="shipped">Shipped</option>
            <option value="delivered">Delivered</option>
            <option value="cancelled">Cancelled</option>
          </select>
          {order.status === 'shipped' && (
            <input
              type="text"
              placeholder="Tracking # (optional)"
              defaultValue={order.trackingNumber || ''}
              onBlur={(e) => {
                const newValue = e.target.value.trim();
                if (newValue !== (order.trackingNumber || '').trim()) {
                  handleTrackingNumberUpdate(order.id, order.status, newValue);
                }
              }}
              className={styles.trackingInput}
            />
          )}
        </div>
      ),
      width: '19%',
    },
  ];

  const totalPages = ordersResult ? Math.ceil(ordersResult.totalCount / ordersResult.pageSize) : 1;

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <h1 className={styles.title}>Orders</h1>
        <div className={styles.actions}>
          <select
            value={statusFilter}
            onChange={(e) => setStatusFilter(e.target.value as OrderStatus | '')}
            className={styles.filterSelect}
          >
            <option value="">All Statuses</option>
            <option value="pending">Pending</option>
            <option value="processing">Processing</option>
            <option value="shipped">Shipped</option>
            <option value="delivered">Delivered</option>
            <option value="cancelled">Cancelled</option>
          </select>
          <Input
            placeholder="Search orders..."
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            className={styles.searchInput}
          />
        </div>
      </div>

      <QueryRenderer
        isLoading={isLoading}
        error={error}
        data={ordersResult?.items}
        emptyTitle="No Orders"
        emptyMessage="No orders found"
        isEmpty={(items) => items.length === 0}
      >
        {(items) => (
          <Card variant="elevated">
            <Table
              columns={columns}
              data={items}
              keyExtractor={(order) => order.id}
            />
            <div className={styles.modalFooter}>
              <Pagination currentPage={page} totalPages={totalPages} onPageChange={setPage} />
            </div>
          </Card>
        )}
      </QueryRenderer>
    </div>
  );
}
