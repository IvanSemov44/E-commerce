import OrderTotalsDisplay from '@/shared/components/OrderTotalsDisplay/OrderTotalsDisplay';
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
    />
  );
}
