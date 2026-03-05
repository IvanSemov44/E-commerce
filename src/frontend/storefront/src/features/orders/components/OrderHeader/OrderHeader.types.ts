export interface OrderHeaderProps {
  orderNumber: string;
  createdAt: string;
  status: 'Pending' | 'Processing' | 'Shipped' | 'Delivered' | 'Cancelled';
  canCancel: boolean;
  isCancelling: boolean;
  onCancel: () => Promise<void> | void;
}
