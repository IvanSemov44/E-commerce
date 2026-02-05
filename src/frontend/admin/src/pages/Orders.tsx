import { useState } from 'react';
import {
  useGetOrdersQuery,
  useUpdateOrderStatusMutation,
} from '../store/api/ordersApi';
import { Card } from '../components/ui/Card';
import Input from '../components/ui/Input';
import Table from '../components/ui/Table';
import Badge from '../components/ui/Badge';
import Pagination from '../components/ui/Pagination';
import styles from './Orders.module.css';
import type { Order, OrderStatus } from '@shared/types';

const statusColors: Record<OrderStatus, 'default' | 'info' | 'warning' | 'success' | 'error'> = {
  pending: 'warning',
  processing: 'info',
  shipped: 'info',
  delivered: 'success',
  cancelled: 'error',
};

export default function Orders() {
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState('');
  const [statusFilter, setStatusFilter] = useState<OrderStatus | ''>('');

  const { data: ordersResult, isLoading, error } = useGetOrdersQuery({
    page,
    pageSize: 20,
    search,
    status: statusFilter || undefined,
  });

  const [updateOrderStatus] = useUpdateOrderStatusMutation();

  const handleStatusChange = async (orderId: string, newStatus: OrderStatus) => {
    try {
      await updateOrderStatus({ orderId, status: newStatus }).unwrap();
      alert('Order status updated successfully');
    } catch (err) {
      alert('Failed to update order status');
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
      accessor: (order: Order) => `$${order.totalAmount.toFixed(2)}`,
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
        <Badge variant={statusColors[order.status]}>
          {order.status}
        </Badge>
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
      accessor: (order: Order) => new Date(order.createdAt).toLocaleDateString(),
      width: '12%',
    },
    {
      header: 'Actions',
      accessor: (order: Order) => (
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
      ),
      width: '19%',
    },
  ];

  const totalPages = ordersResult
    ? Math.ceil(ordersResult.totalCount / ordersResult.pageSize)
    : 1;

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

      <Card variant="elevated">
        {isLoading ? (
          <div className={styles.loadingState}>
            Loading orders...
          </div>
        ) : error ? (
          <div className={styles.errorState}>
            Failed to load orders
          </div>
        ) : (
          <>
            <Table
              columns={columns}
              data={ordersResult?.items || []}
              keyExtractor={(order) => order.id}
            />
            <div className={styles.modalFooter}>
              <Pagination
                currentPage={page}
                totalPages={totalPages}
                onPageChange={setPage}
              />
            </div>
          </>
        )}
      </Card>
    </div>
  );
}
