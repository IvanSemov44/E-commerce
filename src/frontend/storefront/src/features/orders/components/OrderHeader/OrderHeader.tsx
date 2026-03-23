import { useTranslation } from 'react-i18next';

export interface OrderHeaderProps {
  orderNumber: string;
  createdAt: string;
  status: 'Pending' | 'Processing' | 'Shipped' | 'Delivered' | 'Cancelled';
  canCancel: boolean;
  isCancelling: boolean;
  onCancel: () => Promise<void> | void;
}

export function OrderHeader({
  orderNumber,
  createdAt,
  status,
  canCancel,
  isCancelling,
  onCancel,
}: OrderHeaderProps) {
  const { t } = useTranslation();

  const statusColors: Record<string, string> = {
    Pending: 'bg-yellow-100 text-yellow-800',
    Processing: 'bg-blue-100 text-blue-800',
    Shipped: 'bg-purple-100 text-purple-800',
    Delivered: 'bg-green-100 text-green-800',
    Cancelled: 'bg-red-100 text-red-800',
  };

  return (
    <div className="flex flex-col md:flex-row md:items-center md:justify-between gap-4 mb-6">
      <div>
        <h1 className="text-2xl font-bold">
          {t('orders.order')} #{orderNumber}
        </h1>
        <p className="text-gray-500">{new Date(createdAt).toLocaleDateString()}</p>
      </div>
      <div className="flex items-center gap-4">
        <span
          className={`px-3 py-1 rounded-full text-sm font-medium ${statusColors[status] || 'bg-gray-100 text-gray-800'}`}
        >
          {t(`orders.status.${status.toLowerCase()}`)}
        </span>
        {canCancel && (
          <button
            onClick={onCancel}
            disabled={isCancelling}
            className="px-4 py-2 bg-red-500 text-white rounded-lg hover:bg-red-600 disabled:opacity-50"
          >
            {isCancelling
              ? t('orders.cancelling') || 'Cancelling...'
              : t('orders.cancel') || 'Cancel Order'}
          </button>
        )}
      </div>
    </div>
  );
}
