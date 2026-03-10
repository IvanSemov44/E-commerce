import OrderTotalsDisplay from '@/features/orders/components/OrderTotalsDisplay/OrderTotalsDisplay';
import type { OrderTotalsProps } from './OrderTotals.types';

export default function OrderTotals({
  subtotal,
  discountAmount = 0,
  shippingAmount,
  taxAmount,
  totalAmount,
}: OrderTotalsProps) {
  return (
    <OrderTotalsDisplay
      subtotal={subtotal}
      discount={discountAmount}
      shipping={shippingAmount}
      tax={taxAmount}
      total={totalAmount}
      className="bg-gray-50 p-6 rounded-lg mb-6"
    />
  );
}
